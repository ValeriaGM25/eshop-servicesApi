using BuildingBlocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orders.API.Application.CreateOrder;
using Orders.API.Clients;
using Orders.API.Configuration;
using Orders.API.Domain;

namespace Orders.API.Tests;

public sealed class CreateOrderTests
{
    [Fact]
    public async Task Valid_order_creates_pending_order_with_backend_totals()
    {
        var productId = Guid.NewGuid();
        var repository = new InMemoryOrdersRepository();
        var basket = new FakeBasketClient
        {
            Basket = new ShoppingCartDto
            {
                Items =
                [
                    new ShoppingCartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Keyboard",
                        Quantity = 2,
                        Price = 10m
                    }
                ]
            }
        };
        var catalog = new FakeCatalogClient();
        catalog.Products[productId] = new CatalogProductDto { Id = productId, Name = "Keyboard", Price = 12m };
        var handler = CreateHandler(repository, basket, catalog);

        var result = await handler.Handle(new CreateOrderCommand("customer-1", "Valeria Galindo Marin", null, "key-1", "token"), CancellationToken.None);

        Assert.False(result.IsReplay);
        Assert.Equal(OrderStatus.Pending, result.Order.Status);
        Assert.Equal(20m, result.Order.Subtotal);
        Assert.Equal(3.60m, result.Order.Tax);
        Assert.Equal(23.60m, result.Order.Total);
        Assert.Equal("customer-1", result.Order.CustomerId);
        Assert.Equal("Valeria Galindo Marin", result.Order.CustomerName);
        Assert.Single(result.Order.Items);
        Assert.Equal(1, basket.DeleteCalls);
    }

    [Fact]
    public async Task Customer_name_is_persisted_from_authenticated_identity()
    {
        var productId = Guid.NewGuid();
        var repository = new InMemoryOrdersRepository();
        var basket = new FakeBasketClient
        {
            Basket = new ShoppingCartDto
            {
                Items = [new ShoppingCartItemDto { ProductId = productId, ProductName = "SSD", Quantity = 1, Price = 109.99m }]
            }
        };
        var catalog = new FakeCatalogClient();
        catalog.Products[productId] = new CatalogProductDto { Id = productId, Name = "SSD", Price = 109.99m };
        var handler = CreateHandler(repository, basket, catalog);

        var result = await handler.Handle(new CreateOrderCommand("customer-1", "Valeria Galindo Marin", null, "key-identity", "token"), CancellationToken.None);

        Assert.Equal("customer-1", result.Order.CustomerId);
        Assert.Equal("Valeria Galindo Marin", result.Order.CustomerName);
    }

    [Fact]
    public void Create_order_request_does_not_accept_customer_name_from_body()
    {
        var properties = typeof(CreateOrderRequest).GetProperties().Select(property => property.Name);

        Assert.DoesNotContain("CustomerName", properties);
    }

    [Fact]
    public async Task Empty_basket_returns_bad_request()
    {
        var handler = CreateHandler(new InMemoryOrdersRepository(), new FakeBasketClient { Basket = new ShoppingCartDto() }, new FakeCatalogClient());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new CreateOrderCommand("customer-1", "Customer One", null, "key-1", "token"), CancellationToken.None));
    }

    [Fact]
    public async Task Repeated_idempotency_key_returns_same_order_without_duplicate()
    {
        var productId = Guid.NewGuid();
        var repository = new InMemoryOrdersRepository();
        var basket = new FakeBasketClient
        {
            Basket = new ShoppingCartDto
            {
                Items = [new ShoppingCartItemDto { ProductId = productId, ProductName = "Mouse", Quantity = 1, Price = 15m }]
            }
        };
        var catalog = new FakeCatalogClient();
        catalog.Products[productId] = new CatalogProductDto { Id = productId, Name = "Mouse", Price = 15m };
        var handler = CreateHandler(repository, basket, catalog);

        var first = await handler.Handle(new CreateOrderCommand("customer-1", "Customer One", null, "same-key", "token"), CancellationToken.None);
        var second = await handler.Handle(new CreateOrderCommand("customer-1", "Customer One", null, "same-key", "token"), CancellationToken.None);

        Assert.Equal(first.Order.Id, second.Order.Id);
        Assert.True(second.IsReplay);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(1, basket.DeleteCalls);
    }

    [Fact]
    public async Task Missing_product_returns_bad_request()
    {
        var productId = Guid.NewGuid();
        var handler = CreateHandler(
            new InMemoryOrdersRepository(),
            new FakeBasketClient
            {
                Basket = new ShoppingCartDto
                {
                    Items = [new ShoppingCartItemDto { ProductId = productId, ProductName = "Removed", Quantity = 1, Price = 10m }]
                }
            },
            new FakeCatalogClient());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new CreateOrderCommand("customer-1", "Customer One", null, "key-1", "token"), CancellationToken.None));
    }

    [Fact]
    public async Task Mongo_unavailable_returns_controlled_internal_server_error()
    {
        var repository = new InMemoryOrdersRepository { ThrowUnavailable = true };
        var handler = CreateHandler(repository, new FakeBasketClient(), new FakeCatalogClient());

        var exception = await Assert.ThrowsAsync<InternalServerException>(() =>
            handler.Handle(new CreateOrderCommand("customer-1", "Customer One", null, "key-1", "token"), CancellationToken.None));

        Assert.DoesNotContain("mongodb+srv", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CreateOrderCommandHandler CreateHandler(
        InMemoryOrdersRepository repository,
        FakeBasketClient basket,
        FakeCatalogClient catalog)
    {
        return new CreateOrderCommandHandler(
            repository,
            basket,
            catalog,
            Options.Create(new OrdersOptions { TaxRate = 0.18m }),
            NullLogger<CreateOrderCommandHandler>.Instance);
    }
}
