using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Health;

public sealed class ReadinessHealthCheck(ReadinessState readinessState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(readinessState.IsReady
            ? HealthCheckResult.Healthy("Application is ready.")
            : HealthCheckResult.Unhealthy("Application is live but not ready."));
    }
}
