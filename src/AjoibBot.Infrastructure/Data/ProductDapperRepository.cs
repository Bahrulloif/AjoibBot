using AjoibBot.Application.Entities;
using Dapper;
using Npgsql;

namespace AjoibBot.Infrastructure.Data;

public class ProductDapperRepository
{
    private readonly string _connectionString;

    public ProductDapperRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Создаём соединение каждый раз — Dapper не держит DbContext
    private NpgsqlConnection CreateConnection()
        => new NpgsqlConnection(_connectionString);

    // Получить все товары с категорией через JOIN
    public async Task<IEnumerable<ProductWithCategory>> GetAllWithCategoryAsync()
    {
        const string sql = """
            SELECT
                p.id AS Id,
                p.name AS Name,
                p.price AS Price,
                p.size AS Size,
                p.color AS Color,
                p.stock_quantity AS StockQuantity,
                c.name AS CategoryName
            FROM products p
            JOIN categories c ON p.category_id = c.id
            ORDER BY p.id
            """;

        using var connection = CreateConnection();
        return await connection.QueryAsync<ProductWithCategory>(sql);
    }

    // Получить товары дороже заданной цены
    public async Task<IEnumerable<ProductWithCategory>> GetByMinPriceAsync(decimal minPrice)
    {
        const string sql = """
            SELECT
                p.id AS Id,
                p.name AS Name,
                p.price AS Price,
                p.size AS Size,
                c.name AS CategoryName
            FROM products p
            JOIN categories c ON p.category_id = c.id
            WHERE p.price >= @MinPrice
            ORDER BY p.price DESC
            """;

        using var connection = CreateConnection();
        return await connection.QueryAsync<ProductWithCategory>(
            sql, new { MinPrice = minPrice });
    }

    // Статистика по категориям — сложный запрос
    public async Task<IEnumerable<CategoryStats>> GetCategoryStatsAsync()
    {
        const string sql = """
            SELECT
                c.name AS CategoryName,
                COUNT(p.id) AS ProductCount,
                AVG(p.price) AS AvgPrice,
                MAX(p.price) AS MaxPrice,
                SUM(p.stock_quantity) AS TotalStock
            FROM categories c
            LEFT JOIN products p ON p.category_id = c.id
            GROUP BY c.name
            ORDER BY AVG(p.price) DESC
            """;

        using var connection = CreateConnection();
        return await connection.QueryAsync<CategoryStats>(sql);
    }
}

// DTO для результата — не Entity, просто для чтения
public class ProductWithCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Size { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int StockQuantity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CategoryStats
{
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
}