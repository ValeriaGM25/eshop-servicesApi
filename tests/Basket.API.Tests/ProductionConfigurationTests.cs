using Basket.Data;
using BuildingBlocks.Configuration;
using BuildingBlocks.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Basket.API.Tests;

public class ProductionConfigurationTests
{
    [Fact]
    public void MissingDatabase_ThrowsClearError()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.GetRequiredConnectionString("Database"));

        Assert.Equal("Connection string 'Database' is not configured.", exception.Message);
    }

    [Fact]
    public void MissingRedis_ThrowsClearError()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.GetRequiredConnectionString("Redis"));

        Assert.Equal("Connection string 'Redis' is not configured.", exception.Message);
    }

    [Fact]
    public void RedisConnection_IsBuiltFromConfigurationWithResilienceOptions()
    {
        var options = RedisConnectionConfiguration.BuildRedisConfigurationOptions("eshop-redis:6379");

        Assert.Contains(options.EndPoints, endpoint => endpoint.ToString()!.Contains("eshop-redis:6379", StringComparison.Ordinal));
        Assert.False(options.AbortOnConnectFail);
        Assert.True(options.ConnectRetry >= 5);
        Assert.True(options.ConnectTimeout >= 5000);
    }

    [Fact]
    public async Task ReadyHealthCheck_DependsOnReadinessState()
    {
        var readinessState = new ReadinessState();
        var healthCheck = new ReadinessHealthCheck(readinessState);

        var notReady = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        readinessState.MarkReady();
        var ready = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, notReady.Status);
        Assert.Equal(HealthStatus.Healthy, ready.Status);
    }
}
