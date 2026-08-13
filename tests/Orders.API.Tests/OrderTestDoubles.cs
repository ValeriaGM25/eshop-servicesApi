using BuildingBlocks;
using Orders.API.Application;
using Orders.API.Clients;
using Orders.API.Domain;
using Orders.API.Persistence;

namespace Orders.API.Tests;

internal sealed class InMemoryOrdersRepository : IOrdersRepository
{
    private readonly List<Order> orders = [];

    public bool ThrowUnavailable { get; set; }
    public int CreateCalls { get; private set; }

    public Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (ThrowUnavailable)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }

        return Task.FromResult(orders.FirstOrDefault(order => order.Id == id));
    }

    public Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        if (ThrowUnavailable)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }

        return Task.FromResult(orders.FirstOrDefault(order => order.IdempotencyKey == idempotencyKey));
    }

    public Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> result = orders.Where(order => order.CustomerId == customerId).ToList();
        return Task.FromResult(result);
    }

    public Task<PagedResult<Order>> GetByCustomerAsync(string customerId, OrderQueryParameters query, CancellationToken cancellationToken)
    {
        var customerQuery = query.Normalize() with { CustomerId = customerId };
        return GetAsync(customerQuery, cancellationToken);
    }

    public Task<PagedResult<Order>> GetAsync(OrderQueryParameters query, CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        IEnumerable<Order> result = orders;

        if (!string.IsNullOrWhiteSpace(normalized.CustomerId))
        {
            result = result.Where(order => order.CustomerId == normalized.CustomerId);
        }

        if (normalized.Status is not null)
        {
            result = result.Where(order => order.Status == normalized.Status);
        }

        if (normalized.From is not null)
        {
            result = result.Where(order => order.CreatedAt >= normalized.From.Value);
        }

        if (normalized.To is not null)
        {
            result = result.Where(order => order.CreatedAt <= normalized.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            result = result.Where(order =>
                order.Id.Contains(normalized.Search, StringComparison.OrdinalIgnoreCase)
                || order.CustomerId.Contains(normalized.Search, StringComparison.OrdinalIgnoreCase)
                || order.Items.Any(item => item.ProductName.Contains(normalized.Search, StringComparison.OrdinalIgnoreCase)
                    || item.ProductId.ToString().Equals(normalized.Search, StringComparison.OrdinalIgnoreCase)));
        }

        var ordered = result.OrderByDescending(order => order.CreatedAt).ToList();
        var totalItems = ordered.LongCount();
        var items = ordered.Skip((normalized.Page - 1) * normalized.PageSize).Take(normalized.PageSize).ToList();
        var totalPages = totalItems == 0 ? 0 : (long)Math.Ceiling(totalItems / (double)normalized.PageSize);
        return Task.FromResult(new PagedResult<Order>(items, normalized.Page, normalized.PageSize, totalItems, totalPages));
    }

    public Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        if (ThrowUnavailable)
        {
            throw new InternalServerException("Orders persistence is temporarily unavailable.");
        }

        CreateCalls++;
        var existing = orders.FirstOrDefault(current => current.IdempotencyKey == order.IdempotencyKey);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<bool> TryUpdateStatusAsync(string id, OrderStatus expectedStatus, OrderStatus newStatus, CancellationToken cancellationToken)
    {
        var order = orders.FirstOrDefault(current => current.Id == id && current.Status == expectedStatus);
        if (order is null)
        {
            return Task.FromResult(false);
        }

        order.Status = newStatus;
        return Task.FromResult(true);
    }

    public void Add(Order order) => orders.Add(order);
}

internal sealed class FakeBasketClient : IBasketClient
{
    public ShoppingCartDto Basket { get; set; } = new();
    public bool DeleteResult { get; set; } = true;
    public int DeleteCalls { get; private set; }

    public Task<ShoppingCartDto> GetBasketAsync(string bearerToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(Basket);
    }

    public Task<bool> DeleteBasketAsync(string bearerToken, CancellationToken cancellationToken)
    {
        DeleteCalls++;
        return Task.FromResult(DeleteResult);
    }
}

internal sealed class FakeCatalogClient : ICatalogClient
{
    public Dictionary<Guid, CatalogProductDto> Products { get; } = [];

    public Task<CatalogProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        Products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }
}
