using StackExchange.Redis;

public class RedisSecurityService
{
    public static ConfigurationOptions GetSecureOptions(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString, true);

        options.Ssl = false;                    // In production, you can enable SSL by setting this to true
        options.Password = null;                // Set a password if your Redis instance requires authentication
        options.ClientName = "RedisMasteryDemo-Secure";

        // Security best practices
        options.AbortOnConnectFail = false;
        options.AllowAdmin = false;             // Sensitive setting — enable only when necessary
        options.ChannelPrefix = "secure:";

        return options;
    }
}