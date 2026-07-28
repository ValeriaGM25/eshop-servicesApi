using BuildingBlocks.Configuration;
using BuildingBlocks.Health;
using Catalog.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;

namespace Catalog.API.Tests;

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
    public void SeedDemoData_IsDisabledInProductionByDefault()
    {
        var configuration = new ConfigurationBuilder().Build();

        var shouldSeed = CatalogInitialData.ShouldSeedDemoData(configuration, new FakeEnvironment("Production"));

        Assert.False(shouldSeed);
    }

    [Fact]
    public void SeedDemoData_CanBeEnabledExplicitly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseInitialization:SeedDemoData"] = "true"
            })
            .Build();

        var shouldSeed = CatalogInitialData.ShouldSeedDemoData(configuration, new FakeEnvironment("Production"));

        Assert.True(shouldSeed);
    }

    [Fact]
    public void DemoSeedProducts_DoNotContainDuplicateNames()
    {
        var names = CatalogInitialData.GetDemoProducts().Select(product => product.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(8, names.Length);
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
