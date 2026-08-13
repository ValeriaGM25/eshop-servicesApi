namespace Orders.API.Application.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrdersRepository ordersRepository)
    : IQueryHandler<GetOrderByIdQuery, GetOrderByIdResult>
{
    public async Task<GetOrderByIdResult> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await ordersRepository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), query.Id);

        query.Principal.EnsureCanAccessCustomer(order.CustomerId);
        return new GetOrderByIdResult(order);
    }
}
