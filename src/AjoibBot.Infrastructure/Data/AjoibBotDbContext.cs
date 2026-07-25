using AjoibBot.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace AjoibBot.Infrastructure.Data;

public class AjoibBotDbContext : DbContext
{
    // DbContext получает настройки через DI (строка подключения и т.д.)
    public AjoibBotDbContext(DbContextOptions<AjoibBotDbContext> options): base(options)
    {
    }

    // DbSet — это представление таблицы в БД
    // через него делаешь запросы: _context.Products.ToListAsync()
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка таблицы products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");           // имя таблицы в БД
            entity.HasKey(p => p.Id);             // первичный ключ
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Name).HasColumnName("name").IsRequired();
            entity.Property(p => p.Price).HasColumnName("price");
            entity.Property(p => p.Size).HasColumnName("size");
            entity.Property(p => p.Color).HasColumnName("color");
            entity.Property(p => p.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.CategoryId).HasColumnName("category_id");

            // Связь: Product принадлежит одной Category
            // Category имеет много Products
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId);
        });

        // Настройка таблицы categories
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").IsRequired();
        });

        // Настройка таблицы orders
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).HasColumnName("id");
            entity.Property(o => o.ChatId).HasColumnName("chat_id");
            entity.Property(o => o.CustomerName).HasColumnName("customer_name").IsRequired();
            entity.Property(o => o.CustomerPhone).HasColumnName("customer_phone").IsRequired();
            entity.Property(o => o.Status).HasColumnName("status").IsRequired();
            entity.Property(o => o.CreatedAt).HasColumnName("created_at");
        });

        // Настройка таблицы order_items
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.Id).HasColumnName("id");
            entity.Property(oi => oi.OrderId).HasColumnName("order_id");
            entity.Property(oi => oi.ProductId).HasColumnName("product_id");
            entity.Property(oi => oi.ProductName).HasColumnName("product_name").IsRequired();
            entity.Property(oi => oi.UnitPrice).HasColumnName("unit_price");
            entity.Property(oi => oi.Quantity).HasColumnName("quantity");

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId);
        });
    }
}