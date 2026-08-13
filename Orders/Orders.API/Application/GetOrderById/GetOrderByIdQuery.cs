namespace Orders.API.Application.GetOrderById;

public sealed record GetOrderByIdQuery(string Id, ClaimsPrincipal Principal) : IQuery<GetOrderByIdResult>;

public sealed record GetOrderByIdResult(Order Order);
