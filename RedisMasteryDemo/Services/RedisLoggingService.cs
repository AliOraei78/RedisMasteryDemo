using StackExchange.Redis;

public class RedisLoggingService
{
    private readonly ILogger<RedisLoggingService> _logger;
    private readonly IDatabase _db;

    public RedisLoggingService(ILogger<RedisLoggingService> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public async Task LogRedisOperationAsync(string operation, string key, string? value = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Operation = operation,
            Key = key,
            Value = value,
            Server = "localhost:6379"
        };

        _logger.LogInformation("Redis Operation: {Operation} | Key: {Key}", operation, key);

        await _db.ListRightPushAsync("redis:logs", System.Text.Json.JsonSerializer.Serialize(logEntry));
    }
}