using StackExchange.Redis;

public class RateLimitingService
{
    private readonly IDatabase _db;

    private const int TokenCapacity = 10;     // Maximum allowed tokens
    private const int RefillRate = 3;         // Tokens per second
    private const int WindowSeconds = 60;     // Time window (1 minute)

    public RateLimitingService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<bool> IsAllowedAsync(string clientKey)
    {
        string rateKey = $"ratelimit:{clientKey}";
        string timestampKey = $"ratelimit:{clientKey}:ts";

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Using Lua Script for Atomicity (best practice)
        var script = @"
            local tokens = redis.call('GET', KEYS[1])
            local lastRefill = redis.call('GET', KEYS[2])
            
            if not tokens then tokens = ARGV[1] end
            if not lastRefill then lastRefill = ARGV[3] end
            
            local timePassed = ARGV[2] - lastRefill
            local newTokens = math.min(ARGV[1], tokens + (timePassed * ARGV[4] / 60))
            
            if newTokens >= 1 then
                redis.call('SET', KEYS[1], newTokens - 1, 'EX', ARGV[5])
                redis.call('SET', KEYS[2], ARGV[2], 'EX', ARGV[5])
                return 1
            else
                return 0
            end
        ";

        var result = await _db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { rateKey, timestampKey },
            new RedisValue[]
            {
                TokenCapacity,
                now,
                now - WindowSeconds,
                RefillRate,
                WindowSeconds
            });

        return (int)result == 1;
    }

    public async Task<int> GetRemainingTokensAsync(string clientKey)
    {
        var tokens = await _db.StringGetAsync($"ratelimit:{clientKey}");

        return tokens.HasValue
            ? (int)tokens
            : TokenCapacity;
    }
}