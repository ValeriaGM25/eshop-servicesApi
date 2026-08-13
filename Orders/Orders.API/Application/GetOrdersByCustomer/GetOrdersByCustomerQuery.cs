namespace Orders.API.Application.GetOrdersByCustomer;

public sealed record GetOrdersByCustomerQuery(
    string CustomerId,
    ClaimsPrincipal Principal,
    OrderQueryParameters Query) : IQuery<GetOrdersByCustomerResult>;

public sealed record GetOrdersByCustomerResult(IReadOnlyList<Order> Orders, int Page, int PageSize, long TotalItems, long TotalPages);
