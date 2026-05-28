using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(
        builder.Configuration.GetConnectionString("Redis")!, true);
    configuration.AbortOnConnectFail = false;
    configuration.ReconnectRetryPolicy = new ExponentialRetry(5000);
    return ConnectionMultiplexer.Connect(configuration);
});

var app = builder.Build();

app.MapGet("/redis-health", async (IConnectionMultiplexer redis) =>
{
    try
    {
        var db = redis.GetDatabase();
        await db.PingAsync();
        return Results.Ok(new { status = "Redis is connected and healthy!" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis connection failed: {ex.Message}");
    }
});

app.Run();