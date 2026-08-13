using BuildingBlocks;
using Orders.API.Application;
using Orders.API.Application.GetOrderById;
using Orders.API.Application.GetOrders;
using Orders.API.Application.GetOrdersByCustomer;
using Orders.API.Application.UpdateOrderStatus;
using Orders.API.Domain;
using System.Security.Claims;

namespace Orders.API.Tests;

public sealed class OrderQueryAndStatusTests
{
    [Fact]
    public async Task Get_order_returns_complete_data_for_owner()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-1");
        repository.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id, Principal("customer-1", "Cliente")), CancellationToken.None);

        Assert.Equal(order.Id, result.Order.Id);
        Assert.Equal(order.Items[0].ProductId, result.Order.Items[0].ProductId);
        Assert.Equal(order.Total, result.Order.Total);
    }

    [Fact]
    public async Task Get_order_dto_returns_customer_name()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-1", "Valeria Galindo Marin");
        repository.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id, Principal("customer-1", "Cliente")), CancellationToken.None);
        var dto = result.Order.ToDto();

        Assert.Equal("Valeria Galindo Marin", dto.CustomerName);
    }

    [Fact]
    public async Task Customer_orders_returns_only_requested_customer_orders()
    {
        var repository = new InMemoryOrdersRepository();
        repository.Add(SampleOrder("customer-1", "Customer One"));
        repository.Add(SampleOrder("customer-2", "Customer Two"));
        var handler = new GetOrdersByCustomerQueryHandler(repository);

        var result = await handler.Handle(new GetOrdersByCustomerQuery("customer-1", Principal("customer-1", "Cliente"), new OrderQueryParameters()), CancellationToken.None);

        Assert.Single(result.Orders);
        Assert.Equal("customer-1", result.Orders[0].CustomerId);
    }

    [Fact]
    public async Task Admin_can_read_any_customer_order()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-2");
        repository.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id, Principal("admin-1", "Admin")), CancellationToken.None);

        Assert.Equal(order.Id, result.Order.Id);
    }

    [Fact]
    public async Task Admin_lists_orders_with_pagination_summary_data()
    {
        var repository = new InMemoryOrdersRepository();
        repository.Add(SampleOrder("customer-1"));
        repository.Add(SampleOrder("customer-2"));
        var handler = new GetOrdersQueryHandler(repository);

        var result = await handler.Handle(new GetOrdersQuery(new OrderQueryParameters { Page = 1, PageSize = 10 }), CancellationToken.None);

        Assert.Equal(2, result.Orders.TotalItems);
        Assert.Equal(1, result.Orders.Page);
        Assert.Equal(10, result.Orders.PageSize);
        Assert.Contains(result.Orders.Items, order => order.CustomerName == "Customer One");
    }

    [Fact]
    public async Task Admin_list_filters_pending_orders()
    {
        var repository = new InMemoryOrdersRepository();
        repository.Add(SampleOrder("customer-1"));
        var confirmed = SampleOrder("customer-2");
        confirmed.Status = OrderStatus.Confirmed;
        repository.Add(confirmed);
        var handler = new GetOrdersQueryHandler(repository);

        var result = await handler.Handle(new GetOrdersQuery(new OrderQueryParameters { Status = OrderStatus.Pending }), CancellationToken.None);

        Assert.Single(result.Orders.Items);
        Assert.Equal(OrderStatus.Pending, result.Orders.Items[0].Status);
    }

    [Fact]
    public async Task Pending_to_confirmed_returns_updated_order()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-1");
        repository.Add(order);
        var handler = new UpdateOrderStatusCommandHandler(repository);

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Confirmed), CancellationToken.None);

        Assert.Equal(OrderStatus.Confirmed, result.Order.Status);
    }

    [Fact]
    public async Task Pending_to_cancelled_returns_updated_order()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-1");
        repository.Add(order);
        var handler = new UpdateOrderStatusCommandHandler(repository);

        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Cancelled), CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, result.Order.Status);
    }

    [Fact]
    public async Task Invalid_transition_returns_conflict()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-1");
        order.Status = OrderStatus.Cancelled;
        repository.Add(order);
        var handler = new UpdateOrderStatusCommandHandler(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Confirmed), CancellationToken.None));
    }

    [Fact]
    public async Task Customer_cannot_read_another_customer_order()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder("customer-2");
        repository.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetOrderByIdQuery(order.Id, Principal("customer-1", "Cliente")), CancellationToken.None));
    }

    private static Order SampleOrder(string customerId, string customerName = "Customer One")
    {
        return new Order
        {
            CustomerId = customerId,
            CustomerName = customerName,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Keyboard",
                    Quantity = 1,
                    UnitPrice = 10m,
                    LineTotal = 10m
                }
            ],
            Subtotal = 10m,
            Tax = 1.8m,
            Total = 11.8m
        };
    }

    private static ClaimsPrincipal Principal(string customerId, string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, customerId),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));
    }
}
