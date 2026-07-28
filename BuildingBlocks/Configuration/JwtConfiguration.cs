using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BuildingBlocks.Configuration;

public sealed record JwtConfiguration(
    string Issuer,
    string Audience,
    string Key,
    int AccessTokenMinutes,
    int RefreshTokenDays);

public static class JwtConfigurationExtensions
{
    public static JwtConfiguration GetRequiredJwtConfiguration(this IConfiguration configuration)
    {
        var issuer = ReadRequired(configuration, "Jwt:Issuer", "JWT_ISSUER");
        var audience = ReadRequired(configuration, "Jwt:Audience", "JWT_AUDIENCE");
        var key = ReadRequired(configuration, "Jwt:Key", "JWT_KEY");

        return new JwtConfiguration(
            issuer,
            audience,
            key,
            ReadInt(configuration, "Jwt:AccessTokenMinutes", "JWT_ACCESS_TOKEN_MINUTES", 15),
            ReadInt(configuration, "Jwt:RefreshTokenDays", "JWT_REFRESH_TOKEN_DAYS", 7));
    }

    public static TokenValidationParameters CreateTokenValidationParameters(this JwtConfiguration jwtConfiguration)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfiguration.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    }

    private static string ReadRequired(IConfiguration configuration, string canonicalKey, string fallbackKey)
    {
        var value = configuration[canonicalKey] ?? configuration[fallbackKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"JWT configuration is incomplete: {canonicalKey} is missing.");
        }

        return value;
    }

    private static int ReadInt(
        IConfiguration configuration,
        string canonicalKey,
        string fallbackKey,
        int defaultValue)
    {
        var value = configuration[canonicalKey] ?? configuration[fallbackKey];
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
