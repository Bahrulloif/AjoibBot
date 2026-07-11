using System.Text.Json;
using AjoibBot.Application.Entities;
using AjoibBot.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AjoibBot.Infrastructure.Services;

public class CachedProductService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
{
    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
};
    private readonly IProductRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedProductService> _logger;

    // Ключ для кэша
    private const string AllProductsCacheKey = "all_products";
    

    public CachedProductService(
        IProductRepository repository,
        IDistributedCache cache,
        ILogger<CachedProductService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken ct = default)
    {
        // 1. Проверяем кэш
        var cached = await _cache.GetStringAsync(AllProductsCacheKey, ct);

        if (cached != null)
        {
            // Данные есть в кэше — возвращаем без запроса в БД
            _logger.LogInformation(">>> Данные из REDIS кэша");
            return JsonSerializer.Deserialize<List<Product>>(cached, _jsonOptions)!;
        }

        // 2. Кэша нет — идём в БД
        _logger.LogInformation(">>> Данные из БАЗЫ ДАННЫХ");
        var products = await _repository.GetAllAsync(ct);

        // 3. Сохраняем в кэш на 2 минуты
        await _cache.SetStringAsync(
            AllProductsCacheKey,
            JsonSerializer.Serialize(products, _jsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            },
            ct);

        return products;
    }

    public async Task InvalidateCacheAsync(CancellationToken ct = default)
    {
        // Удаляем кэш когда данные изменились
        await _cache.RemoveAsync(AllProductsCacheKey, ct);
    }
}