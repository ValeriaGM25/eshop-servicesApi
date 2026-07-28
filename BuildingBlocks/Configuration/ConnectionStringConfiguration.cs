using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Configuration;

public static class ConnectionStringConfiguration
{
    public static string GetRequiredConnectionString(
        this IConfiguration configuration,
        string name)
    {
        var connectionString = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{name}' is not configured.");
        }

        return connectionString;
    }
}
