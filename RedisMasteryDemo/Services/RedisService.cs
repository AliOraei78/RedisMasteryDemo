using StackExchange.Redis;
using System.Text.Json;

public class RedisService
{
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // String Operations
    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, value, (Expiration)expiry);

    public async Task<string?> GetStringAsync(string key)
        => await _db.StringGetAsync(key);

    // List Operations
    public async Task<long> AddToListAsync(string key, string value)
        => await _db.ListRightPushAsync(key, value);

    public async Task<List<string>> GetListAsync(string key)
        => (await _db.ListRangeAsync(key)).Select(x => x.ToString()).ToList();

    // Set Operations
    public async Task AddToSetAsync(string key, string value)
        => await _db.SetAddAsync(key, value);

    public async Task<HashSet<string>> GetSetAsync(string key)
        => (await _db.SetMembersAsync(key)).Select(x => x.ToString()).ToHashSet();

    // Hash Operations
    public async Task SetHashAsync(string key, string field, string value)
        => await _db.HashSetAsync(key, field, value);

    public async Task<Dictionary<string, string>> GetHashAsync(string key)
    {
        var hash = await _db.HashGetAllAsync(key);
        return hash.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
    }

    // Sorted Set Operations
    public async Task AddToSortedSetAsync(string key, string member, double score)
        => await _db.SortedSetAddAsync(key, member, score);

    public async Task<List<LeaderboardEntry>> GetTopFromSortedSetAsync(string key, long count = 10)
    {
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending);
        return entries.Select(e => new LeaderboardEntry
        {
            UserName = e.Element.ToString(),
            Score = (int)e.Score
        }).ToList();
    }
}