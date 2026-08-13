namespace Orders.API.Application.GetOrders;

public sealed class GetOrdersQueryHandler(IOrdersRepository ordersRepository)
    : IQueryHandler<GetOrdersQuery, GetOrdersResult>
{
    public async Task<GetOrdersResult> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await ordersRepository.GetAsync(query.Query, cancellationToken);
        return new GetOrdersResult(orders);
    }
}
