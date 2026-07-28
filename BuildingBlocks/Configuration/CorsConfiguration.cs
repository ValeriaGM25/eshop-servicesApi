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
            .Select(NormalizeOrigin)
            .Select(ValidateOrigin)
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

    private static string NormalizeOrigin(string origin)
    {
        return origin.Trim().TrimEnd('/');
    }

    private static string ValidateOrigin(string origin)
    {
        if (origin.Contains('*', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CORS configuration is invalid: wildcard origins are not allowed.");
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Scheme)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrWhiteSpace(uri.PathAndQuery.Trim('/'))
            || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            throw new InvalidOperationException("CORS configuration is invalid: Cors:AllowedOrigins must contain absolute origins only.");
        }

        return origin;
    }
}
