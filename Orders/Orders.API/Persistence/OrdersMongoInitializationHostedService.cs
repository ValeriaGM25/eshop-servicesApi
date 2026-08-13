using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Orders.API.Persistence;

public sealed class OrdersMongoInitializationHostedService(
    IMongoClient mongoClient,
    IOptions<MongoDbOptions> options,
    ReadinessState readinessState,
    ILogger<OrdersMongoInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var mongoOptions = options.Value;
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        var collection = database.GetCollection<Order>(mongoOptions.OrdersCollection);

        try
        {
            var indexes = new[]
            {
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(order => order.IdempotencyKey),
                    new CreateIndexOptions { Unique = true, Name = "ux_orders_idempotency_key" }),
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(order => order.CustomerId),
                    new CreateIndexOptions { Name = "ix_orders_customer_id" }),
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Descending(order => order.CreatedAt),
                    new CreateIndexOptions { Name = "ix_orders_created_at" })
            };

            await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
            await database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            readinessState.MarkReady();
            logger.LogInformation("Orders MongoDB indexes created and readiness marked.");
        }
        catch (MongoException exception)
        {
            logger.LogError(exception, "Orders MongoDB initialization failed without exposing the connection string.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
