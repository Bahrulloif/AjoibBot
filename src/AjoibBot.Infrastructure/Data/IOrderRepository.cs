using AjoibBot.Application.Entities;

namespace AjoibBot.Infrastructure.Data;

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderResult
{
    public Order? Order { get; set; }
    public List<int> UnknownProductIds { get; set; } = new();
    public List<int> InsufficientStockProductIds { get; set; } = new();

    public bool IsSuccess => Order is not null;
}

public interface IOrderRepository
{
    // Проверяет наличие, списывает остаток и создаёт заказ — всё в одном SaveChangesAsync
    Task<CreateOrderResult> CreateAsync(
        long chatId,
        string customerName,
        string customerPhone,
        IReadOnlyList<OrderItemRequest> items,
        CancellationToken ct = default);

    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
}
