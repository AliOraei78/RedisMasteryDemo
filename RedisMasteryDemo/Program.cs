using Microsoft.AspNetCore.Mvc;
using RedisMasteryDemo.Interfaces;
using RedisMasteryDemo.Models;
using RedisMasteryDemo.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(
        builder.Configuration.GetConnectionString("Redis")!, true);
    config.AbortOnConnectFail = false;
    config.ConnectTimeout = 10000;
    config.SyncTimeout = 10000;
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);        // Session expiration
    options.Cookie.HttpOnly = true;                        // Security
    options.Cookie.IsEssential = true;                     // GDPR compliance
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSingleton<RedisService>();
builder.Services.AddSingleton<RateLimitingService>();
builder.Services.AddSingleton<DistributedCacheService>();
builder.Services.AddSingleton<RedisPubSubService>();
builder.Services.AddSingleton<RedisOptimizationService>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RedisMasteryDemo_";
});

builder.Services.AddSingleton<ProductRepository>(); 
builder.Services.AddScoped<IProductRepository>(sp =>
{
    var innerRepo = sp.GetRequiredService<ProductRepository>();
    var redisService = sp.GetRequiredService<RedisService>();
    return new CachedProductRepository(innerRepo, redisService);
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});

var app = builder.Build();

// Rate Limiting Middleware
app.UseMiddleware<RateLimitingMiddleware>();
app.UseSession();
var distributedCache = app.Services.GetRequiredService<DistributedCacheService>();
var optimizationService = app.Services.GetRequiredService<RedisOptimizationService>();

app.MapGet("/redis-health", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    await db.PingAsync();
    return Results.Ok(new { status = "Redis is healthy" });
});

var redisService = app.Services.GetRequiredService<RedisService>();

// ====================== String Operations ======================
app.MapPost("/string/{key}", async (string key, [FromBody] string value) =>
{
    await redisService.SetStringAsync(key, value, TimeSpan.FromMinutes(10));
    return Results.Ok($"String with key '{key}' saved successfully.");
});

app.MapGet("/string/{key}", async (string key) =>
    Results.Ok(new { key, value = await redisService.GetStringAsync(key) }));

// ====================== List Operations ======================
app.MapPost("/list/{key}", async (string key, [FromBody] string value) =>
{
    await redisService.AddToListAsync(key, value);
    return Results.Ok($"Item added to list '{key}'.");
});

app.MapGet("/list/{key}", async (string key) =>
    Results.Ok(await redisService.GetListAsync(key)));

// ====================== Set Operations ======================
app.MapPost("/set/{key}", async (string key, [FromBody] string value) =>
{
    await redisService.AddToSetAsync(key, value);
    return Results.Ok($"Item added to Set '{key}'.");
});

app.MapGet("/set/{key}", async (string key) =>
    Results.Ok(await redisService.GetSetAsync(key)));

// ====================== Hash Operations ======================
app.MapPost("/hash/{key}", async (string key, [FromBody] Dictionary<string, string> data) =>
{
    foreach (var item in data)
    {
        await redisService.SetHashAsync(key, item.Key, item.Value);
    }
    return Results.Ok($"Data saved to Hash '{key}'.");
});

app.MapGet("/hash/{key}", async (string key) =>
    Results.Ok(await redisService.GetHashAsync(key)));

// ====================== Leaderboard (Sorted Set) ======================
app.MapPost("/leaderboard", async ([FromBody] LeaderboardEntry entry) =>
{
    await redisService.AddToSortedSetAsync("leaderboard:scores", entry.UserName, entry.Score);
    return Results.Ok($"Score for user {entry.UserName} has been recorded.");
});

app.MapGet("/leaderboard", async () =>
    Results.Ok(await redisService.GetTopFromSortedSetAsync("leaderboard:scores", 10)));

// Product Endpoints
app.MapGet("/products", async (IProductRepository repo) =>
    Results.Ok(await repo.GetAllAsync()));

app.MapGet("/products/{id}", async (int id, IProductRepository repo) =>
{
    var product = await repo.GetByIdAsync(id);
    return product != null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/products", async (Product product, IProductRepository repo) =>
{
    await repo.AddAsync(product);
    return Results.Created($"/products/{product.Id}", product);
});

// ====================== Distributed Cache Endpoints ======================

app.MapPost("/distributed/string/{key}", async (string key, [FromBody] string value, DistributedCacheService cacheService) =>
{
    await cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));
    return Results.Ok($"Value with key '{key}' has been saved in the Distributed Cache.");
});

app.MapGet("/distributed/string/{key}", async (string key, DistributedCacheService cacheService) =>
{
    var value = await cacheService.GetAsync<string>(key);
    return Results.Ok(new { key, value });
});

app.MapPost("/distributed/product", async ([FromBody] Product product, DistributedCacheService cacheService) =>
{
    await cacheService.SetAsync($"product:{product.Id}", product,
        absoluteExpiration: TimeSpan.FromMinutes(15),
        slidingExpiration: TimeSpan.FromMinutes(5));

    return Results.Ok($"Product {product.Id} has been saved with Absolute and Sliding Expiration.");
});

app.MapGet("/distributed/product/{id}", async (int id, DistributedCacheService cacheService) =>
{
    var product = await cacheService.GetAsync<Product>($"product:{id}");
    return product != null ? Results.Ok(product) : Results.NotFound();
});

app.MapDelete("/distributed/{key}", async (string key, DistributedCacheService cacheService) =>
{
    await cacheService.RemoveAsync(key);
    return Results.Ok($"Key '{key}' has been removed from the Cache.");
});

// ====================== Session Management Endpoints ======================

app.MapPost("/session/set", async (HttpContext context, [FromBody] Dictionary<string, string> data) =>
{
    foreach (var item in data)
    {
        context.Session.SetString(item.Key, item.Value);
    }

    return Results.Ok("Data has been stored in the Session.");
});

app.MapGet("/session/get/{key}", (HttpContext context, string key) =>
{
    var value = context.Session.GetString(key);

    return Results.Ok(new
    {
        key,
        value,
        sessionId = context.Session.Id
    });
});

app.MapGet("/session/all", (HttpContext context) =>
{
    var sessionData = new Dictionary<string, string>();

    // Note: In Minimal API, Session does not directly expose all keys
    // For demonstration purposes, we show only sample information
    return Results.Ok(new
    {
        sessionId = context.Session.Id,
        message = "Use Redis Insight to view all stored values."
    });
});

app.MapPost("/session/clear", (HttpContext context) =>
{
    context.Session.Clear();

    return Results.Ok("Session has been cleared.");
});

app.MapGet("/ratelimit/test", () =>
{
    return Results.Ok(new
    {
        message = "Request received successfully.",
        time = DateTime.Now
    });
});

app.MapGet("/ratelimit/status", async (RateLimitingService rateService) =>
{
    string clientKey = "test-client";
    var remaining = await rateService.GetRemainingTokensAsync(clientKey);
    return Results.Ok(new { clientKey, remainingTokens = remaining });
});

var pubSubService = app.Services.GetRequiredService<RedisPubSubService>();

// ====================== Pub/Sub Endpoints ======================

// Publish message
app.MapPost("/pubsub/publish/{channel}",
    async (
        string channel,
        [FromBody] Message message,
        RedisPubSubService service) =>
    {
        message.Channel = channel;
        message.Timestamp = DateTime.UtcNow;

        await service.PublishAsync(channel, message);

        return Results.Ok(new
        {
            status = "Message published",
            channel,
            messageId = message.Id
        });
    });

// Subscribe (for testing - logs to console)
app.MapGet("/pubsub/subscribe/{channel}",
    (string channel, RedisPubSubService service) =>
    {
        service.Subscribe(channel, message =>
        {
            Console.WriteLine(
                $"📨 New message received in channel '{channel}':");

            Console.WriteLine(
                $"From: {message.Sender} | Content: {message.Content}");

            Console.WriteLine(
                $"Time: {message.Timestamp}");
        });

        return Results.Ok(
            $"Subscribed to channel: {channel}. Messages will appear in the Output Window.");
    });

// Send test message to notifications channel
app.MapPost("/pubsub/notify",
    async (RedisPubSubService service) =>
    {
        var message = new Message
        {
            Sender = "System",
            Content = "New notification: A new product has been added to the store!",
            Channel = "notifications"
        };

        await service.PublishAsync("notifications", message);

        return Results.Ok("Notification published.");
    });

// Transaction
app.MapPost(
    "/optimization/transaction/{id}",
    async (
        int id,
        [FromBody] Product product,
        RedisOptimizationService service
    ) =>
    {
        product.Id = id;

        var success = await service.UpdateProductWithTransactionAsync(product);

        return Results.Ok(new
        {
            success,
            message = "Transaction completed successfully"
        });
    }
);

// Pipelining
app.MapGet(
    "/optimization/pipeline",
    async (RedisOptimizationService service) =>
    {
        var keys = new List<string>
        {
            "product:1",
            "product:2",
            "product:3"
        };

        var values = await service.GetMultipleStringsWithPipelineAsync(keys);

        return Results.Ok(values);
    }
);

// Benchmark
app.MapGet(
    "/optimization/benchmark",
    async (RedisOptimizationService service) =>
    {
        var result = await service.BenchmarkAsync();

        return Results.Ok(result);
    }
);

app.Run();