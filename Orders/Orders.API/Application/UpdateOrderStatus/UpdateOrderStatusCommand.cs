namespace Orders.API.Application.UpdateOrderStatus;

public sealed record UpdateOrderStatusRequest(OrderStatus Status);

public sealed record UpdateOrderStatusCommand(string Id, OrderStatus Status) : ICommand<UpdateOrderStatusResult>;

public sealed record UpdateOrderStatusResult(Order Order);
