using AjoibBot.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace AjoibBot.Infrastructure.Data;

public class OrderRepository : IOrderRepository
{
    private readonly AjoibBotDbContext _context;

    public OrderRepository(AjoibBotDbContext context)
    {
        _context = context;
    }

    public async Task<CreateOrderResult> CreateAsync(
        long chatId,
        string customerName,
        string customerPhone,
        IReadOnlyList<OrderItemRequest> items,
        CancellationToken ct = default)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        // Трекаемые сущности — StockQuantity нужно изменить и сохранить в этой же транзакции
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var unknownProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        var insufficientStockProductIds = items
            .Where(i => products.TryGetValue(i.ProductId, out var p) && p.StockQuantity < i.Quantity)
            .Select(i => i.ProductId)
            .ToList();

        if (unknownProductIds.Count > 0 || insufficientStockProductIds.Count > 0)
        {
            return new CreateOrderResult
            {
                UnknownProductIds = unknownProductIds,
                InsufficientStockProductIds = insufficientStockProductIds
            };
        }

        var order = new Order
        {
            ChatId = chatId,
            CustomerName = customerName,
            CustomerPhone = customerPhone
        };

        foreach (var item in items)
        {
            var product = products[item.ProductId];
            product.StockQuantity -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        return new CreateOrderResult { Order = order };
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }
}
