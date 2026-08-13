namespace Orders.API.Application.CreateOrder;

public sealed record CreateOrderRequest(string CustomerId, string? BasketId);

public sealed record CreateOrderResponse(OrderDto Order, bool IsReplay);

public sealed record CreateOrderCommand(
    string CustomerId,
    string CustomerName,
    string? BasketId,
    string IdempotencyKey,
    string BearerToken) : ICommand<CreateOrderResult>;

public sealed record CreateOrderResult(Order Order, bool IsReplay);
