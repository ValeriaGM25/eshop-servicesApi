namespace Orders.API.Application.GetOrdersByCustomer;

public sealed class GetOrdersByCustomerQueryHandler(IOrdersRepository ordersRepository)
    : IQueryHandler<GetOrdersByCustomerQuery, GetOrdersByCustomerResult>
{
    public async Task<GetOrdersByCustomerResult> Handle(GetOrdersByCustomerQuery query, CancellationToken cancellationToken)
    {
        query.Principal.EnsureCanAccessCustomer(query.CustomerId);
        var orders = await ordersRepository.GetByCustomerAsync(query.CustomerId, query.Query, cancellationToken);
        return new GetOrdersByCustomerResult(orders.Items, orders.Page, orders.PageSize, orders.TotalItems, orders.TotalPages);
    }
}
