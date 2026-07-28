using BuildingBlocks.Configuration;
using BuildingBlocks.Health;
using Identity.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;

namespace Identity.API.Tests;

public class ProductionConfigurationTests
{
    [Fact]
    public void MissingIdentityDatabase_ThrowsClearError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.GetRequiredConnectionString("IdentityDatabase"));

        Assert.Equal("Connection string 'IdentityDatabase' is not configured.", exception.Message);
    }

    [Fact]
    public void CanonicalJwtConfiguration_IsResolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:Key"] = "this-is-a-test-key-with-enough-length",
                ["Jwt:AccessTokenMinutes"] = "20",
                ["Jwt:RefreshTokenDays"] = "10"
            })
            .Build();

        var jwt = configuration.GetRequiredJwtConfiguration();

        Assert.Equal("issuer", jwt.Issuer);
        Assert.Equal("audience", jwt.Audience);
        Assert.Equal(20, jwt.AccessTokenMinutes);
        Assert.Equal(10, jwt.RefreshTokenDays);
    }

    [Fact]
    public void LegacyJwtFallback_IsResolved()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_ISSUER"] = "legacy-issuer",
                ["JWT_AUDIENCE"] = "legacy-audience",
                ["JWT_KEY"] = "this-is-a-legacy-test-key-with-enough-length",
                ["JWT_ACCESS_TOKEN_MINUTES"] = "30",
                ["JWT_REFRESH_TOKEN_DAYS"] = "14"
            })
            .Build();

        var jwt = configuration.GetRequiredJwtConfiguration();

        Assert.Equal("legacy-issuer", jwt.Issuer);
        Assert.Equal("legacy-audience", jwt.Audience);
        Assert.Equal(30, jwt.AccessTokenMinutes);
        Assert.Equal(14, jwt.RefreshTokenDays);
    }

    [Fact]
    public void ProductionRefreshCookie_IsSecureSameSiteNoneAndHttpOnly()
    {
        var options = TokenService.CreateRefreshTokenCookieOptions(new FakeEnvironment("Production"));

        Assert.True(options.HttpOnly);
        Assert.True(options.Secure);
        Assert.Equal(SameSiteMode.None, options.SameSite);
        Assert.Equal("/auth", options.Path);
    }

    [Fact]
    public void DevelopmentRefreshCookie_IsLaxAndNotSecure()
    {
        var options = TokenService.CreateRefreshTokenCookieOptions(new FakeEnvironment("Development"));

        Assert.True(options.HttpOnly);
        Assert.False(options.Secure);
        Assert.Equal(SameSiteMode.Lax, options.SameSite);
        Assert.Equal("/auth", options.Path);
    }

    [Fact]
    public async Task ReadinessHealthCheck_IsNotReadyBeforeInitialization()
    {
        var readinessState = new ReadinessState();
        var healthCheck = new ReadinessHealthCheck(readinessState);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ReadinessHealthCheck_IsReadyAfterInitialization()
    {
        var readinessState = new ReadinessState();
        readinessState.MarkReady();
        var healthCheck = new ReadinessHealthCheck(readinessState);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
