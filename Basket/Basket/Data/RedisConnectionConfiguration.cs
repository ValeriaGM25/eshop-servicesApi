namespace Basket.Data;

public static class RedisConnectionConfiguration
{
    public static string BuildRedisConnectionString(string connectionString)
    {
        var options = BuildRedisConfigurationOptions(connectionString);
        return options.ToString();
    }

    public static ConfigurationOptions BuildRedisConfigurationOptions(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;

        if (options.ConnectRetry < 5)
        {
            options.ConnectRetry = 5;
        }

        if (options.ConnectTimeout < 5000)
        {
            options.ConnectTimeout = 5000;
        }

        return options;
    }
}
