using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace Orders.API.Persistence;

public sealed class MongoOrdersRepository : IOrdersRepository
{
    private readonly IMongoCollection<Order> orders;

    public MongoOrdersRepository(IMongoClient mongoClient, IOptions<MongoDbOptions> options)
    {
        var mongoOptions = options.Value;
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
        orders = database.GetCollection<Order>(mongoOptions.OrdersCollection);
    }

    public async Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await orders.Find(order => order.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        return await orders.Find(order => order.IdempotencyKey == idempotencyKey).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        return await orders
            .Find(order => order.CustomerId == customerId)
            .SortByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Order>> GetByCustomerAsync(
        string customerId,
        OrderQueryParameters query,
        CancellationToken cancellationToken)
    {
        var customerQuery = query.Normalize() with { CustomerId = customerId };
        return await GetAsync(customerQuery, cancellationToken);
    }

    public async Task<PagedResult<Order>> GetAsync(OrderQueryParameters query, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = query.Normalize();
            var filter = BuildFilter(normalized);
            var totalItems = await orders.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var pageItems = await orders
                .Find(filter)
                .SortByDescending(order => order.CreatedAt)
                .Skip((normalized.Page - 1) * normalized.PageSize)
                .Limit(normalized.PageSize)
                .ToListAsync(cancellationToken);

            var totalPages = totalItems == 0 ? 0 : (long)Math.Ceiling(totalItems / (double)normalized.PageSize);
            return new PagedResult<Order>(pageItems, normalized.Page, normalized.PageSize, totalItems, totalPages);
        }
        catch (MongoException)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            await orders.InsertOneAsync(order, cancellationToken: cancellationToken);
            return order;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await GetByIdempotencyKeyAsync(order.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            throw new InternalServerException("Order could not be created due to a persistence conflict.");
        }
        catch (MongoException)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }
    }

    public async Task<bool> TryUpdateStatusAsync(
        string id,
        OrderStatus expectedStatus,
        OrderStatus newStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var update = Builders<Order>.Update.Set(order => order.Status, newStatus);
            var result = await orders.UpdateOneAsync(
                order => order.Id == id && order.Status == expectedStatus,
                update,
                cancellationToken: cancellationToken);

            return result.ModifiedCount == 1;
        }
        catch (MongoException)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }
    }

    private static FilterDefinition<Order> BuildFilter(OrderQueryParameters query)
    {
        var builder = Builders<Order>.Filter;
        var filters = new List<FilterDefinition<Order>>();

        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            filters.Add(builder.Eq(order => order.CustomerId, query.CustomerId));
        }

        if (query.Status is not null)
        {
            filters.Add(builder.Eq(order => order.Status, query.Status.Value));
        }

        if (query.From is not null)
        {
            filters.Add(builder.Gte(order => order.CreatedAt, query.From.Value));
        }

        if (query.To is not null)
        {
            filters.Add(builder.Lte(order => order.CreatedAt, query.To.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var regex = new BsonRegularExpression(Regex.Escape(query.Search), "i");
            var searchFilters = new List<FilterDefinition<Order>>
            {
                builder.Regex(order => order.Id, regex),
                builder.Regex(order => order.CustomerId, regex),
                builder.Regex("Items.ProductName", regex)
            };

            if (Guid.TryParse(query.Search, out var productId))
            {
                searchFilters.Add(builder.Eq("Items.ProductId", productId));
            }

            filters.Add(builder.Or(searchFilters));
        }

        return filters.Count == 0 ? builder.Empty : builder.And(filters);
    }
}
