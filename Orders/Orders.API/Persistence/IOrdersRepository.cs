namespace Orders.API.Persistence;

public interface IOrdersRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<PagedResult<Order>> GetByCustomerAsync(string customerId, OrderQueryParameters query, CancellationToken cancellationToken);
    Task<PagedResult<Order>> GetAsync(OrderQueryParameters query, CancellationToken cancellationToken);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken);
    Task<bool> TryUpdateStatusAsync(string id, OrderStatus expectedStatus, OrderStatus newStatus, CancellationToken cancellationToken);
}
