using RedisMasteryDemo.Interfaces;
using RedisMasteryDemo.Models;
using StackExchange.Redis;
using System.Text.Json;

public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _innerRepository;
    private readonly RedisService _redisService;
    private const string CacheKeyPrefix = "product:";

    public CachedProductRepository(IProductRepository innerRepository, RedisService redisService)
    {
        _innerRepository = innerRepository;
        _redisService = redisService;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        string cacheKey = $"{CacheKeyPrefix}{id}";

        // Try reading from cache
        var cached = await _redisService.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<Product>(cached);
        }

        // Cache miss → fetch from main source
        var product = await _innerRepository.GetByIdAsync(id);
        if (product != null)
        {
            // Store in cache (10 minutes expiration)
            await _redisService.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(product),
                TimeSpan.FromMinutes(10));
        }

        return product;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        string cacheKey = $"{CacheKeyPrefix}all";

        var cached = await _redisService.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<List<Product>>(cached) ?? new List<Product>();
        }

        var products = await _innerRepository.GetAllAsync();

        await _redisService.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(products),
            TimeSpan.FromMinutes(5));

        return products;
    }

    public async Task AddAsync(Product product)
    {
        await _innerRepository.AddAsync(product);
        await InvalidateCacheAsync(); // Clear cache after write operation
    }

    public async Task UpdateAsync(Product product)
    {
        await _innerRepository.UpdateAsync(product);

        await InvalidateCacheAsync();

        await _redisService.SetStringAsync(
            $"{CacheKeyPrefix}{product.Id}",
            JsonSerializer.Serialize(product),
            TimeSpan.FromMinutes(10));
    }

    public async Task DeleteAsync(int id)
    {
        await _innerRepository.DeleteAsync(id);
        await InvalidateCacheAsync();
    }

    private async Task InvalidateCacheAsync()
    {
        // In real-world projects, tag-based invalidation is recommended
        await _redisService.SetStringAsync($"{CacheKeyPrefix}all", "", TimeSpan.FromSeconds(1));
    }
}