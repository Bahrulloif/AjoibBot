using AjoibBot.Admin.Api.Controllers;
using AjoibBot.Application.Entities;
using AjoibBot.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AjoibBot.Tests.Controllers;

public class ProductsControllerTests
{
    // Общие переменные для всех тестов
    private readonly Mock<IProductRepository> _mockRepo;
    private readonly ProductsController _controller;

    // Конструктор — выполняется перед каждым тестом
    public ProductsControllerTests()
    {
        _mockRepo = new Mock<IProductRepository>();
        _controller = new ProductsController(_mockRepo.Object);
    }

    // ─── Тест 1 ───────────────────────────────────────────
    [Fact]
    public async Task GetAll_ReturnsOk_WithListOfProducts()
    {
        // Arrange — подготовка
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Платье Снежинка", Price = 120 },
            new Product { Id = 2, Name = "Костюм Спорт",   Price = 180 },
        };

        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(products);

        // Act — выполнение
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert — проверка
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should()
            .BeAssignableTo<List<Product>>().Subject;

        returnedProducts.Should().HaveCount(2);
        returnedProducts.First().Name.Should().Be("Платье Снежинка");
    }

    // ─── Тест 2 ───────────────────────────────────────────
    [Fact]
    public async Task GetById_ReturnsOk_WhenProductExists()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Платье Снежинка", Price = 120 };

        _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProduct = okResult.Value.Should()
            .BeAssignableTo<Product>().Subject;

        returnedProduct.Id.Should().Be(1);
        returnedProduct.Name.Should().Be("Платье Снежинка");
    }

    // ─── Тест 3 ───────────────────────────────────────────
    [Fact]
    public async Task GetById_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange — репозиторий вернёт null (товар не найден)
        _mockRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert — должен вернуть 404
        result.Should().BeOfType<NotFoundResult>();
    }

    // ─── Тест 4 ───────────────────────────────────────────
    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoProducts()
    {
        // Arrange — пустой список
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Product>());

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should()
            .BeAssignableTo<List<Product>>().Subject;

        returnedProducts.Should().BeEmpty();
    }
}