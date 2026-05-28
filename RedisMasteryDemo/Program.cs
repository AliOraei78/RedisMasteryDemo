using Microsoft.AspNetCore.Mvc;
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

var app = builder.Build();

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

app.Run();