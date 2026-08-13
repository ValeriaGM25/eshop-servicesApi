using Microsoft.Extensions.Options;

namespace Orders.API.Application.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IOrdersRepository ordersRepository,
    IBasketClient basketClient,
    ICatalogClient catalogClient,
    IOptions<OrdersOptions> options,
    ILogger<CreateOrderCommandHandler> logger) : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var existing = await ordersRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return new CreateOrderResult(existing, true);
        }

        var basket = await basketClient.GetBasketAsync(command.BearerToken, cancellationToken);
        if (basket.Items.Count == 0)
        {
            throw new BadRequestException("Basket must contain at least one product before creating an order.");
        }

        var orderItems = new List<OrderItem>();
        foreach (var basketItem in basket.Items)
        {
            ValidateBasketItem(basketItem);
            var product = await catalogClient.GetProductAsync(basketItem.ProductId, cancellationToken);
            if (product is null)
            {
                throw new BadRequestException($"Product {basketItem.ProductId} does not exist.");
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                throw new BadRequestException($"Product {basketItem.ProductId} is inconsistent.");
            }

            var unitPrice = decimal.Round(basketItem.Price, 2, MidpointRounding.AwayFromZero);
            var lineTotal = decimal.Round(unitPrice * basketItem.Quantity, 2, MidpointRounding.AwayFromZero);
            orderItems.Add(new OrderItem
            {
                ProductId = basketItem.ProductId,
                ProductName = string.IsNullOrWhiteSpace(basketItem.ProductName) ? product.Name : basketItem.ProductName,
                Quantity = basketItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        var subtotal = decimal.Round(orderItems.Sum(item => item.LineTotal), 2, MidpointRounding.AwayFromZero);
        var tax = decimal.Round(subtotal * options.Value.TaxRate, 2, MidpointRounding.AwayFromZero);
        var order = new Order
        {
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Items = orderItems,
            Subtotal = subtotal,
            Tax = tax,
            Total = subtotal + tax,
            IdempotencyKey = command.IdempotencyKey
        };

        var persisted = await ordersRepository.CreateAsync(order, cancellationToken);
        var isReplay = persisted.Id != order.Id;
        if (!isReplay)
        {
            var deleted = await basketClient.DeleteBasketAsync(command.BearerToken, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Order {OrderId} was persisted but basket cleanup failed. Idempotency prevents duplicate order creation on retry.", persisted.Id);
            }
        }

        return new CreateOrderResult(persisted, isReplay);
    }

    private static void ValidateBasketItem(ShoppingCartItemDto item)
    {
        if (item.ProductId == Guid.Empty)
        {
            throw new BadRequestException("Basket contains an invalid ProductId.");
        }

        if (item.Quantity <= 0)
        {
            throw new BadRequestException($"Product {item.ProductId} has an invalid quantity.");
        }

        if (item.Price <= 0)
        {
            throw new BadRequestException($"Product {item.ProductId} has an invalid price.");
        }
    }
}
