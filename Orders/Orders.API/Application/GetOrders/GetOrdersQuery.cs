namespace Orders.API.Application.GetOrders;

public sealed record GetOrdersQuery(OrderQueryParameters Query) : IQuery<GetOrdersResult>;

public sealed record GetOrdersResult(PagedResult<Order> Orders);
