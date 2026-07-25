using System.Text.Json;
using AjoibBot.Application.Entities;
using AjoibBot.Infrastructure.Data;
using AjoibBot.Infrastructure.Services.OpenAi;
using FluentAssertions;

namespace AjoibBot.Tests.Services;

public class CatalogAssistantServiceTests
{
    private static List<Product> BuildCatalog() =>
    [
        new Product { Id = 1, Name = "Платье Снежинка", Price = 120, Category = new Category { Name = "Платья" } },
        new Product { Id = 2, Name = "Костюм Спорт", Price = 350, Category = new Category { Name = "Костюмы" } },
        new Product { Id = 3, Name = "Платье Летнее", Price = 90, Category = new Category { Name = "Платья" } },
    ];

    // ─── search_products ───────────────────────────────────
    [Fact]
    public void SearchProducts_FiltersByQuery()
    {
        var products = BuildCatalog();
        var args = BinaryData.FromString("""{ "query": "Платье" }""");

        var json = CatalogAssistantService.SearchProducts(products, args);
        var results = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        results.Should().HaveCount(2);
        results.Select(r => r.GetProperty("Name").GetString())
            .Should().BeEquivalentTo("Платье Снежинка", "Платье Летнее");
    }

    [Fact]
    public void SearchProducts_FiltersByCategoryAndMaxPrice()
    {
        var products = BuildCatalog();
        var args = BinaryData.FromString("""{ "categoryName": "Платья", "maxPrice": 100 }""");

        var json = CatalogAssistantService.SearchProducts(products, args);
        var results = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        results.Should().ContainSingle();
        results[0].GetProperty("Name").GetString().Should().Be("Платье Летнее");
    }

    [Fact]
    public void SearchProducts_ReturnsEmptyArray_WhenNothingMatches()
    {
        var products = BuildCatalog();
        var args = BinaryData.FromString("""{ "query": "Не существует" }""");

        var json = CatalogAssistantService.SearchProducts(products, args);
        var results = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchProducts_ReturnsAll_WhenNoFiltersProvided()
    {
        var products = BuildCatalog();
        var args = BinaryData.FromString("{}");

        var json = CatalogAssistantService.SearchProducts(products, args);
        var results = JsonSerializer.Deserialize<List<JsonElement>>(json)!;

        results.Should().HaveCount(3);
    }

    // ─── list_categories ───────────────────────────────────
    [Fact]
    public void ListCategories_ReturnsDistinctSortedNames()
    {
        var products = BuildCatalog();

        var json = CatalogAssistantService.ListCategories(products);
        var categories = JsonSerializer.Deserialize<List<string>>(json)!;

        categories.Should().Equal("Костюмы", "Платья");
    }

    // ─── create_order (парсинг аргументов) ──────────────────
    [Fact]
    public void ParseCreateOrderRequest_ParsesAllFields()
    {
        var args = BinaryData.FromString("""
            {
                "customerName": "Иван",
                "customerPhone": "+992900000000",
                "items": [
                    { "productId": 1, "quantity": 2 },
                    { "productId": 3, "quantity": 1 }
                ]
            }
            """);

        var request = CatalogAssistantService.ParseCreateOrderRequest(args);

        request.CustomerName.Should().Be("Иван");
        request.CustomerPhone.Should().Be("+992900000000");
        request.Items.Should().BeEquivalentTo(
        [
            new OrderItemRequest { ProductId = 1, Quantity = 2 },
            new OrderItemRequest { ProductId = 3, Quantity = 1 }
        ]);
    }

    [Fact]
    public void ParseCreateOrderRequest_ReturnsEmptyDefaults_WhenFieldsMissing()
    {
        var args = BinaryData.FromString("{}");

        var request = CatalogAssistantService.ParseCreateOrderRequest(args);

        request.CustomerName.Should().BeEmpty();
        request.CustomerPhone.Should().BeEmpty();
        request.Items.Should().BeEmpty();
    }

    [Fact]
    public void ParseCreateOrderRequest_SkipsMalformedItems()
    {
        var args = BinaryData.FromString("""
            {
                "customerName": "Иван",
                "customerPhone": "+992900000000",
                "items": [
                    { "productId": 1, "quantity": 2 },
                    { "productId": "не число", "quantity": 1 },
                    { "quantity": 5 }
                ]
            }
            """);

        var request = CatalogAssistantService.ParseCreateOrderRequest(args);

        request.Items.Should().BeEquivalentTo(
        [
            new OrderItemRequest { ProductId = 1, Quantity = 2 }
        ]);
    }
}
