using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Orders.API.Persistence;

public sealed class OrdersMongoHealthCheck(IMongoClient mongoClient, IOptions<MongoDbOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = mongoClient.GetDatabase(options.Value.DatabaseName);
            await database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Orders MongoDB is reachable.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Orders MongoDB is not reachable.");
        }
    }
}
