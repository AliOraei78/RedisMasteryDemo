using Polly;
using Polly.Retry;
using StackExchange.Redis;

public class RedisResilientService
{
    private readonly IDatabase _db;
    private readonly AsyncRetryPolicy _retryPolicy;

    public RedisResilientService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();

        _retryPolicy = Policy
            .Handle<RedisConnectionException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromMilliseconds(200 * retryAttempt));
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(Func<IDatabase, Task<T>> operation)
    {
        return await _retryPolicy.ExecuteAsync(() => operation(_db));
    }

    public async Task ExecuteWithResilienceAsync(Func<IDatabase, Task> operation)
    {
        await _retryPolicy.ExecuteAsync(() => operation(_db));
    }
}