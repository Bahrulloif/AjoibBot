using System.Collections.Concurrent;
using System.Text.Json;
using AjoibBot.Application.Entities;
using AjoibBot.Application.Interfaces;
using AjoibBot.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Telegram.Bot;

namespace AjoibBot.Infrastructure.Services.OpenAi;

// Ведёт диалог покупателя с ботом через OpenAI, используя function calling
// для чтения каталога из БД и оформления заказа — модель не должна выдумывать
// товары/цены и не должна оформлять заказ без подтверждения покупателя.
public class CatalogAssistantService
{
    private const int MaxHistoryMessages = 20;
    private const int MaxToolCallRounds = 4;

    private const string SystemPrompt = """
        Ты — консультант интернет-магазина детской одежды AjoibBot в Telegram.
        Отвечай дружелюбно, кратко и по делу, на русском языке.

        Для любых вопросов о товарах, ценах, размерах, цветах, наличии и категориях
        ОБЯЗАТЕЛЬНО вызывай инструменты search_products / list_categories — никогда не
        выдумывай товары, цены или наличие, используй только данные из инструментов.

        Если покупатель хочет оформить заказ:
        1. Через search_products узнай точные id нужных товаров.
        2. Уточни у покупателя количество каждого товара, имя и телефон для связи.
        3. Проговори итоговый список товаров, количество и сумму и дождись явного
           подтверждения покупателя ("да", "оформляй" и т.п.) — без подтверждения
           create_order вызывать нельзя.
        4. Только после подтверждения вызови create_order с id товаров, количеством,
           именем и телефоном.
        5. Если create_order вернул ошибку (товара не хватает или id не найден) —
           сообщи об этом покупателю и предложи альтернативу или другое количество.
        6. После успешного создания заказа сообщи покупателю номер заказа и что
           продавец с ним свяжется.

        Если по запросу ничего не нашлось — сообщи об этом и предложи уточнить запрос.
        """;

    private static readonly ChatTool SearchProductsTool = ChatTool.CreateFunctionTool(
        functionName: "search_products",
        functionDescription: "Ищет товары в каталоге по названию, категории и/или максимальной цене.",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "query": { "type": "string", "description": "Часть названия товара для поиска" },
                    "categoryName": { "type": "string", "description": "Название категории товара" },
                    "maxPrice": { "type": "number", "description": "Максимальная цена товара" }
                }
            }
            """u8.ToArray()));

    private static readonly ChatTool ListCategoriesTool = ChatTool.CreateFunctionTool(
        functionName: "list_categories",
        functionDescription: "Возвращает список всех категорий товаров, доступных в каталоге.");

    private static readonly ChatTool CreateOrderTool = ChatTool.CreateFunctionTool(
        functionName: "create_order",
        functionDescription: "Оформляет заказ покупателя. Вызывать только после того, как покупатель подтвердил состав и сумму заказа.",
        functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "customerName": { "type": "string", "description": "Имя покупателя" },
                    "customerPhone": { "type": "string", "description": "Телефон покупателя для связи" },
                    "items": {
                        "type": "array",
                        "description": "Список товаров в заказе",
                        "items": {
                            "type": "object",
                            "properties": {
                                "productId": { "type": "integer", "description": "Id товара, полученный из search_products" },
                                "quantity": { "type": "integer", "description": "Количество" }
                            },
                            "required": ["productId", "quantity"]
                        }
                    }
                },
                "required": ["customerName", "customerPhone", "items"]
            }
            """u8.ToArray()));

    private readonly ChatClient _chatClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _bot;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CatalogAssistantService> _logger;
    private readonly ConcurrentDictionary<long, List<ChatMessage>> _history = new();

    public CatalogAssistantService(
        ChatClient chatClient,
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient bot,
        IConfiguration configuration,
        ILogger<CatalogAssistantService> logger)
    {
        _chatClient = chatClient;
        _scopeFactory = scopeFactory;
        _bot = bot;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetReplyAsync(long chatId, string userText, CancellationToken ct = default)
    {
        var messages = _history.GetOrAdd(chatId, _ => new List<ChatMessage> { new SystemChatMessage(SystemPrompt) });
        messages.Add(new UserChatMessage(userText));

        var options = new ChatCompletionOptions
        {
            Tools = { SearchProductsTool, ListCategoriesTool, CreateOrderTool }
        };

        var replyText = string.Empty;

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, ct);
            messages.Add(new AssistantChatMessage(completion));

            if (completion.FinishReason != ChatFinishReason.ToolCalls)
            {
                replyText = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
                break;
            }

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var products = await repository.GetAllAsync(ct);

            foreach (var toolCall in completion.ToolCalls)
            {
                var result = await ExecuteToolAsync(toolCall, products, orderRepository, chatId, ct);
                messages.Add(new ToolChatMessage(toolCall.Id, result));
            }
        }

        TrimHistory(messages);

        if (string.IsNullOrWhiteSpace(replyText))
        {
            _logger.LogWarning("Assistant did not produce a final reply for chat {ChatId}", chatId);
            return "Извини, не получилось сформулировать ответ. Попробуй переформулировать вопрос.";
        }

        return replyText;
    }

    private async Task<string> ExecuteToolAsync(
        ChatToolCall toolCall,
        IReadOnlyList<Product> products,
        IOrderRepository orderRepository,
        long chatId,
        CancellationToken ct)
    {
        return toolCall.FunctionName switch
        {
            "search_products" => SearchProducts(products, toolCall.FunctionArguments),
            "list_categories" => ListCategories(products),
            "create_order" => await CreateOrderAsync(chatId, orderRepository, toolCall.FunctionArguments, ct),
            _ => JsonSerializer.Serialize(new { error = $"Неизвестный инструмент: {toolCall.FunctionName}" })
        };
    }

    public static string SearchProducts(IReadOnlyList<Product> products, BinaryData argumentsJson)
    {
        string? query = null;
        string? categoryName = null;
        decimal? maxPrice = null;

        using (var document = JsonDocument.Parse(argumentsJson))
        {
            var root = document.RootElement;
            if (root.TryGetProperty("query", out var q) && q.ValueKind == JsonValueKind.String)
                query = q.GetString();
            if (root.TryGetProperty("categoryName", out var c) && c.ValueKind == JsonValueKind.String)
                categoryName = c.GetString();
            if (root.TryGetProperty("maxPrice", out var m) && m.ValueKind == JsonValueKind.Number)
                maxPrice = m.GetDecimal();
        }

        var matches = products
            .Where(p =>
                (string.IsNullOrWhiteSpace(query) || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(categoryName) || p.Category.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase)) &&
                (maxPrice is null || p.Price <= maxPrice))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Size,
                p.Color,
                p.StockQuantity,
                CategoryName = p.Category.Name
            })
            .ToList();

        return JsonSerializer.Serialize(matches);
    }

    public static string ListCategories(IReadOnlyList<Product> products)
    {
        var categories = products
            .Select(p => p.Category.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        return JsonSerializer.Serialize(categories);
    }

    public static CreateOrderRequest ParseCreateOrderRequest(BinaryData argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var root = document.RootElement;

        var customerName = root.TryGetProperty("customerName", out var n) && n.ValueKind == JsonValueKind.String
            ? n.GetString() ?? string.Empty
            : string.Empty;
        var customerPhone = root.TryGetProperty("customerPhone", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? string.Empty
            : string.Empty;

        var items = new List<OrderItemRequest>();
        if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemEl in itemsEl.EnumerateArray())
            {
                if (itemEl.TryGetProperty("productId", out var pid) &&
                    itemEl.TryGetProperty("quantity", out var qty) &&
                    pid.ValueKind == JsonValueKind.Number &&
                    qty.ValueKind == JsonValueKind.Number)
                {
                    items.Add(new OrderItemRequest { ProductId = pid.GetInt32(), Quantity = qty.GetInt32() });
                }
            }
        }

        return new CreateOrderRequest
        {
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Items = items
        };
    }

    private async Task<string> CreateOrderAsync(
        long chatId,
        IOrderRepository orderRepository,
        BinaryData argumentsJson,
        CancellationToken ct)
    {
        var request = ParseCreateOrderRequest(argumentsJson);

        if (string.IsNullOrWhiteSpace(request.CustomerName) ||
            string.IsNullOrWhiteSpace(request.CustomerPhone) ||
            request.Items.Count == 0)
        {
            return JsonSerializer.Serialize(new
            {
                error = "Не хватает данных для заказа: нужны имя, телефон и хотя бы один товар с productId и quantity."
            });
        }

        var result = await orderRepository.CreateAsync(
            chatId, request.CustomerName, request.CustomerPhone, request.Items, ct);

        if (!result.IsSuccess)
        {
            return JsonSerializer.Serialize(new
            {
                error = "Не удалось оформить заказ.",
                unknownProductIds = result.UnknownProductIds,
                insufficientStockProductIds = result.InsufficientStockProductIds
            });
        }

        var order = result.Order!;
        await NotifySellerAsync(order, ct);

        return JsonSerializer.Serialize(new
        {
            orderId = order.Id,
            status = "confirmed",
            total = order.Items.Sum(i => i.UnitPrice * i.Quantity)
        });
    }

    private async Task NotifySellerAsync(Order order, CancellationToken ct)
    {
        var adminChatId = _configuration.GetValue<long?>("Telegram:AdminChatId");
        if (adminChatId is null or 0)
        {
            _logger.LogWarning(
                "Telegram:AdminChatId не задан — уведомление о заказе {OrderId} не отправлено", order.Id);
            return;
        }

        var itemsText = string.Join(
            '\n', order.Items.Select(i => $"• {i.ProductName} × {i.Quantity} = {i.UnitPrice * i.Quantity:0.##}"));
        var total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        var text = $"""
            🆕 Новый заказ #{order.Id}
            Покупатель: {order.CustomerName}
            Телефон: {order.CustomerPhone}

            {itemsText}

            Итого: {total:0.##}
            """;

        try
        {
            await _bot.SendMessage(adminChatId.Value, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить продавцу уведомление о заказе {OrderId}", order.Id);
        }
    }

    private static void TrimHistory(List<ChatMessage> messages)
    {
        // messages[0] — системный промпт, его всегда сохраняем
        if (messages.Count <= MaxHistoryMessages + 1)
            return;

        var trimmed = new List<ChatMessage> { messages[0] };
        trimmed.AddRange(messages.Skip(messages.Count - MaxHistoryMessages));
        messages.Clear();
        messages.AddRange(trimmed);
    }
}

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = new();
}
