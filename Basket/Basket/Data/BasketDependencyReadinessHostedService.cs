using BuildingBlocks.Health;
using Npgsql;

namespace Basket.Data;

public sealed class BasketDependencyReadinessHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ReadinessState readinessState,
    ILogger<BasketDependencyReadinessHostedService> logger) : BackgroundService
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
                logger.LogInformation("Starting Basket dependency readiness check. Attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                await using var postgresConnection = new NpgsqlConnection(configuration.GetRequiredConnectionString("Database"));
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(ConnectionTimeout);

                logger.LogInformation("Testing Basket PostgreSQL connection.");
                await postgresConnection.OpenAsync(timeoutCts.Token);
                logger.LogInformation("Basket PostgreSQL connection succeeded.");

                using var scope = serviceProvider.CreateScope();
                var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                logger.LogInformation("Testing Basket Redis connection.");
                await redis.GetDatabase().PingAsync();
                logger.LogInformation("Basket Redis connection succeeded.");

                readinessState.MarkReady();
                logger.LogInformation("Basket dependencies are ready. Application is ready.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Basket dependency readiness check failed on attempt {Attempt} of {MaxAttempts}.", attempt, MaxAttempts);

                if (attempt == MaxAttempts)
                {
                    logger.LogError("Basket dependency readiness failed after {MaxAttempts} attempts. Application is live but not ready.", MaxAttempts);
                    return;
                }

                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}
