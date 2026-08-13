namespace Orders.API.Application.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler(IOrdersRepository ordersRepository)
    : ICommandHandler<UpdateOrderStatusCommand, UpdateOrderStatusResult>
{
    public async Task<UpdateOrderStatusResult> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var current = await ordersRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.Id);

        if (current.Status != OrderStatus.Pending || command.Status is not (OrderStatus.Confirmed or OrderStatus.Cancelled))
        {
            throw new ConflictException($"Transition {current.Status} -> {command.Status} is not allowed.");
        }

        var updated = await ordersRepository.TryUpdateStatusAsync(command.Id, OrderStatus.Pending, command.Status, cancellationToken);
        if (!updated)
        {
            throw new ConflictException("Order status could not be updated because the current status changed.");
        }

        var order = await ordersRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), command.Id);

        return new UpdateOrderStatusResult(order);
    }
}
