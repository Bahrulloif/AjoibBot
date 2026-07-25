using AjoibBot.Admin.Api.Controllers;
using AjoibBot.Application.Entities;
using AjoibBot.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AjoibBot.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderRepository> _mockRepo;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockRepo = new Mock<IOrderRepository>();
        _controller = new OrdersController(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithListOfOrders()
    {
        var orders = new List<Order>
        {
            new Order { Id = 1, CustomerName = "Иван", CustomerPhone = "+992900000000" },
            new Order { Id = 2, CustomerName = "Мария", CustomerPhone = "+992900000001" },
        };

        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(orders);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrders = okResult.Value.Should().BeAssignableTo<List<Order>>().Subject;

        returnedOrders.Should().HaveCount(2);
        returnedOrders.First().CustomerName.Should().Be("Иван");
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoOrders()
    {
        _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Order>());

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrders = okResult.Value.Should().BeAssignableTo<List<Order>>().Subject;

        returnedOrders.Should().BeEmpty();
    }
}
