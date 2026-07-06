using AjoibBot.Application.Entities;


namespace AjoibBot.Infrastructure.Data;


public interface IProductRepository
{ // Получить все товары с категорией
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    // Получить товар по id
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    // Получить товары по категории
    Task<List<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);

    // Запись (новое)
    Task<Product> CreateAsync(Product product, CancellationToken ct = default);
    Task<Product?> UpdateAsync(int id, Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}