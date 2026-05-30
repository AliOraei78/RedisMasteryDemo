using StackExchange.Redis;
using Polly;
using Polly.Retry;

public class RedisConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisConfigurationService> _logger;

    public RedisConfigurationService(IConfiguration configuration, ILogger<RedisConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ConfigurationOptions GetConfigurationOptions()
    {
        var redisSettings = _configuration.GetSection("RedisSettings");
        var options = ConfigurationOptions.Parse(redisSettings["ConnectionString"]!, true);

        options.ClientName = "RedisMasteryDemo";
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = int.Parse(redisSettings["CommandTimeout"]!);
        options.SyncTimeout = int.Parse(redisSettings["CommandTimeout"]!);
        options.AsyncTimeout = int.Parse(redisSettings["CommandTimeout"]!);
        options.KeepAlive = int.Parse(redisSettings["KeepAlive"]!);
        options.ReconnectRetryPolicy = new ExponentialRetry(int.Parse(redisSettings["ReconnectRetryDelayMs"]!));

        return options;
    }
}