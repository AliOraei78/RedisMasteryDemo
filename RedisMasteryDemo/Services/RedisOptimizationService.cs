using RedisMasteryDemo.Models;
using StackExchange.Redis;
using System.Text.Json;

public class RedisOptimizationService
{
    private readonly IDatabase _db;

    public RedisOptimizationService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ==================== Transaction ====================
    public async Task<bool> UpdateProductWithTransactionAsync(Product product)
    {
        var transaction = _db.CreateTransaction();

        // Multiple operations executed atomically
        await transaction.HashSetAsync($"product:{product.Id}", "Name", product.Name).ConfigureAwait(false);
        await transaction.HashSetAsync($"product:{product.Id}", "Price", product.Price.ToString()).ConfigureAwait(false);
        await transaction.HashSetAsync($"product:{product.Id}", "Stock", product.Stock.ToString()).ConfigureAwait(false);
        await transaction.StringSetAsync(
            $"product:lastupdate:{product.Id}",
            DateTime.UtcNow.ToString()
        ).ConfigureAwait(false);

        var result = await transaction.ExecuteAsync();
        return result;
    }

    // ==================== Pipelining ====================
    public async Task<List<string>> GetMultipleStringsWithPipelineAsync(List<string> keys)
    {
        var batch = _db.CreateBatch();

        var tasks = keys.Select(key => batch.StringGetAsync(key)).ToList();

        batch.Execute();

        var results = await Task.WhenAll(tasks);

        return results.Select(x => x.ToString()).ToList();
    }

    // ==================== Batch Operations ====================
    public async Task<long> AddMultipleToSetAsync(string setKey, List<string> values)
    {
        var valuesRedis = values.Select(v => (RedisValue)v).ToArray();

        return await _db.SetAddAsync(setKey, valuesRedis);
    }

    // Simple performance benchmark
    public async Task<Dictionary<string, long>> BenchmarkAsync()
    {
        var results = new Dictionary<string, long>();

        // Test without pipeline
        var start = DateTime.UtcNow;

        for (int i = 0; i < 100; i++)
        {
            await _db.StringSetAsync($"test:normal:{i}", i);
        }

        results["Normal"] = (DateTime.UtcNow - start).Milliseconds;

        // Test with pipeline
        start = DateTime.UtcNow;

        var batch = _db.CreateBatch();

        for (int i = 0; i < 100; i++)
        {
            await batch.StringSetAsync($"test:pipeline:{i}", i).ConfigureAwait(false);
        }

        batch.Execute();

        results["Pipeline"] = (DateTime.UtcNow - start).Milliseconds;

        return results;
    }
}