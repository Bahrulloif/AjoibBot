using AjoibBot.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace AjoibBot.Infrastructure.Data;

public class ProductRepository : IProductRepository
{
    private readonly AjoibBotDbContext _context;

    public ProductRepository(AjoibBotDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync(ct);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        // Добавляем объект в DbContext — EF Core начинает его отслеживать
        _context.Products.Add(product);

        // SaveChangesAsync — выполняет INSERT в БД
        // EF Core сам генерирует SQL и заполняет product.Id после вставки
        await _context.SaveChangesAsync(ct);

        return product; // теперь у product есть Id от БД
    }

    public async Task<Product?> UpdateAsync(int id, Product product, CancellationToken ct = default)
    {
        // Ищем существующий товар в БД
        var existing = await _context.Products.FindAsync(new object[] { id }, ct);

        if (existing is null)
            return null; // товар не найден — вернём null

        // Обновляем поля существующего объекта
        existing.Name = product.Name;
        existing.Price = product.Price;
        existing.Size = product.Size;
        existing.Color = product.Color;
        existing.StockQuantity = product.StockQuantity;
        existing.CategoryId = product.CategoryId;

        // SaveChangesAsync — выполняет UPDATE в БД
        await _context.SaveChangesAsync(ct);

        return existing; // возвращаем обновлённый объект
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        // Ищем товар в БД
        var product = await _context.Products.FindAsync(new object[] { id }, ct);

        if (product is null)
            return false; // не нашли — возвращаем false

        // Помечаем для удаления
        _context.Products.Remove(product);

        // SaveChangesAsync — выполняет DELETE в БД
        await _context.SaveChangesAsync(ct);

        return true; // удалили успешно
    }
}