using AjoibBot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoibBot.Admin.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;

    public OrdersController(IOrderRepository repository)
    {
        _repository = repository;
    }

    // GET /api/orders — история заказов, оформленных через бота
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _repository.GetAllAsync(ct);
        return Ok(orders);
    }
}
