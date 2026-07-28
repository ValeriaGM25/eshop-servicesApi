using BuildingBlocks.Health;
using Npgsql;

namespace Catalog.API.Data;

public sealed class CatalogDatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ReadinessState readinessState,
    ILogger<CatalogDatabaseInitializationHostedService> logger) : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkNotReady();

        for (var attempt = 1; attempt <= MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                logger.LogInformation("Starting Catalog database initialization. Attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                var connectionString = configuration.GetRequiredConnectionString("Database");
                await using var connection = new NpgsqlConnection(connectionString);
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(ConnectionTimeout);

                logger.LogInformation("Testing Catalog database connection.");
                await connection.OpenAsync(timeoutCts.Token);
                logger.LogInformation("Catalog database connection succeeded.");

                using var scope = serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

                logger.LogInformation("Catalog Marten storage initialization started.");
                await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
                logger.LogInformation("Catalog Marten storage initialization completed.");

                if (CatalogInitialData.ShouldSeedDemoData(configuration, scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>()))
                {
                    logger.LogInformation("Catalog demo data seed started.");
                    await CatalogInitialData.SeedAsync(scope.ServiceProvider, createScope: false);
                    logger.LogInformation("Catalog demo data seed completed.");
                }

                readinessState.MarkReady();
                logger.LogInformation("Catalog database initialization completed. Application is ready.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Catalog database initialization failed on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    logger.LogError("Catalog database initialization failed after {MaxAttempts} attempts. Application is live but not ready.", MaxAttempts);
                    return;
                }

                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}
