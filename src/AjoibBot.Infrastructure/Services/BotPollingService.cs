using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace AjoibBot.Infrastructure.Services;

public class BotPollingService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<BotPollingService> _logger;

    public BotPollingService(ITelegramBotClient bot, ILogger<BotPollingService> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting bot polling service...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
        };

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: ct
        );

        await Task.Delay(Timeout.Infinite, ct); // Keep the service running until cancellation is requested
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient bot,
        Update update,
        CancellationToken ct)
    {
        // Handle incoming updates here (e.g., messages, commands)
        if (update.Message is not { Text: { } text } message)
            return;
        _logger.LogInformation("Received message from {ChatId}: {Text}",
         message.From?.Id, text);

        var chatId = update.Message.Chat.Id;
        // Example: Echo the received message back to the user
        await bot.SendMessage(chatId, $"You said: {text}", cancellationToken: ct);

    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "An error occurred while processing an update.");
        return Task.CompletedTask;
    }
}