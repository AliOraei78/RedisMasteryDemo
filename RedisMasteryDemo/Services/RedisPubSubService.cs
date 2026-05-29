using StackExchange.Redis;
using System.Text.Json;

public class RedisPubSubService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;

    public RedisPubSubService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _subscriber = redis.GetSubscriber();
    }

    // Publish message
    public async Task PublishAsync(string channel, Message message)
    {
        var json = JsonSerializer.Serialize(message);

        await _subscriber.PublishAsync(channel, json);
    }

    // Subscribe to a Channel
    public void Subscribe(
        string channel,
        Action<Message> onMessageReceived)
    {
        _subscriber.Subscribe(channel, (redisChannel, redisValue) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<Message>(
                    redisValue.ToString());

                if (message != null)
                {
                    onMessageReceived(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error processing message: {ex.Message}");
            }
        });
    }

    public async Task UnsubscribeAsync(string channel)
    {
        await _subscriber.UnsubscribeAsync(channel);
    }
}