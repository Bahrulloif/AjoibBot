using AjoibBot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoibBot.Admin.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ProductDapperRepository _dapper;

    public ReportsController(ProductDapperRepository dapper)
    {
        _dapper = dapper;
    }

    // GET /api/reports/products
    [HttpGet("products")]
    public async Task<IActionResult> GetAllProducts()
    {
        var products = await _dapper.GetAllWithCategoryAsync();
        return Ok(products);
    }

    // GET /api/reports/products/by-price?minPrice=100
    [HttpGet("products/by-price")]
    public async Task<IActionResult> GetByPrice([FromQuery] decimal minPrice = 0)
    {
        var products = await _dapper.GetByMinPriceAsync(minPrice);
        return Ok(products);
    }

    // GET /api/reports/categories/stats
    [HttpGet("categories/stats")]
    public async Task<IActionResult> GetCategoryStats()
    {
        var stats = await _dapper.GetCategoryStatsAsync();
        return Ok(stats);
    }
}