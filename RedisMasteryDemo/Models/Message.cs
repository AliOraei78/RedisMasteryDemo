public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Channel { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}