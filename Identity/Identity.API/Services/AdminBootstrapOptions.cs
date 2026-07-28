namespace Identity.API.Services;

using Microsoft.Extensions.Hosting;

public sealed record AdminBootstrapOptions(
    string? Email,
    string? Password,
    string FullName,
    bool ForcePasswordReset)
{
    private const string DefaultFullName = "Administrator";

    public static AdminBootstrapOptions FromConfiguration(IConfiguration configuration)
    {
        return new AdminBootstrapOptions(
            NormalizeEmail(configuration["AUTH_ADMIN_EMAIL"]),
            configuration["AUTH_ADMIN_PASSWORD"],
            string.IsNullOrWhiteSpace(configuration["AUTH_ADMIN_FULL_NAME"])
                ? DefaultFullName
                : configuration["AUTH_ADMIN_FULL_NAME"]!.Trim(),
            bool.TryParse(configuration["AUTH_ADMIN_FORCE_PASSWORD_RESET"], out var forcePasswordReset) && forcePasswordReset);
    }

    public void ValidateForProduction(IWebHostEnvironment environment)
    {
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(Email))
        {
            throw new InvalidOperationException("AUTH_ADMIN_EMAIL is not configured.");
        }
    }

    public string GetRequiredPasswordForCreate()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("AUTH_ADMIN_PASSWORD is required to create the initial administrator.");
        }

        return Password;
    }

    public string GetRequiredPasswordForReset()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("AUTH_ADMIN_PASSWORD is required when password reset is enabled.");
        }

        return Password;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
