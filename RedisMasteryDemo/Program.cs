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

builder.Services.AddSingleton<RedisService>();

builder.Services.AddSingleton<DistributedCacheService>();

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

var app = builder.Build();

var distributedCache = app.Services.GetRequiredService<DistributedCacheService>();

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

app.Run();