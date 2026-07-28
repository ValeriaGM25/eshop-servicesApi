using BuildingBlocks.Health;

namespace Identity.API.Services;

public sealed class IdentityDatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    ReadinessState readinessState,
    ILogger<IdentityDatabaseInitializationHostedService> logger) : BackgroundService
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
                logger.LogInformation("Starting Identity database initialization. Attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

                logger.LogInformation("Testing Identity database connection.");
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(ConnectionTimeout);

                if (!await dbContext.Database.CanConnectAsync(timeoutCts.Token))
                {
                    throw new InvalidOperationException("Identity database connection test failed.");
                }

                logger.LogInformation("Identity database connection succeeded.");
                logger.LogInformation("Identity database migration started.");
                await dbContext.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Identity database migration completed.");

                logger.LogInformation("Identity database seed started.");
                await IdentitySeeder.SeedAsync(scope.ServiceProvider, createScope: false);
                logger.LogInformation("Identity database seed completed.");

                readinessState.MarkReady();
                logger.LogInformation("Identity database initialization completed. Application is ready.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Identity database initialization failed on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    logger.LogError("Identity database initialization failed after {MaxAttempts} attempts. Application is live but not ready.", MaxAttempts);
                    return;
                }

                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}
