using AjoibBot.Application.DTOs;
using AjoibBot.Application.Entities;
using AjoibBot.Infrastructure.Data;
using AjoibBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoibBot.Admin.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly CachedProductService _cachedService;

    public ProductsController(
        IProductRepository repository,
        CachedProductService cachedService)
    {
        _repository = repository;
        _cachedService = cachedService;
    }

    // GET /api/products — теперь через Redis кэш
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _cachedService.GetAllAsync(ct);
        return Ok(products);
    }

    // GET /api/products/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        if (product is null)
            return NotFound();
        return Ok(product);
    }

    // GET /api/products/category/3
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(int categoryId, CancellationToken ct)
    {
        var products = await _repository.GetByCategoryAsync(categoryId, ct);
        return Ok(products);
    }

    // POST /api/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Size = dto.Size,
            Color = dto.Color,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(product, ct);

        // Инвалидируем кэш — данные изменились
        await _cachedService.InvalidateCacheAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Size = dto.Size,
            Color = dto.Color,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId
        };

        var updated = await _repository.UpdateAsync(id, product, ct);
        if (updated is null)
            return NotFound();

        // Инвалидируем кэш
        await _cachedService.InvalidateCacheAsync(ct);

        return Ok(updated);
    }

    // DELETE /api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        // Инвалидируем кэш
        await _cachedService.InvalidateCacheAsync(ct);

        return NoContent();
    }
}