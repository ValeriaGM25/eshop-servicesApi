using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Configuration;

public static class CorsConfiguration
{
    private static readonly string[] DevelopmentOrigins =
    [
        "http://localhost:5173",
        "http://localhost:8088"
    ];

    public static string[] GetRequiredCorsOrigins(
        this IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (configuredOrigins.Length > 0)
        {
            return configuredOrigins;
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentOrigins;
        }

        throw new InvalidOperationException("CORS configuration is incomplete: Cors:AllowedOrigins is missing.");
    }
}
