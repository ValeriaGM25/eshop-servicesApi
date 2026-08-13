using MongoDB.Bson;
using Orders.API.Domain;

namespace Orders.API.Tests;

public sealed class OrderBsonSerializationTests
{
    [Fact]
    public void Order_with_real_items_serializes_to_bson()
    {
        var productId = Guid.NewGuid();
        var order = new Order
        {
            CustomerId = "customer-1",
            CustomerName = "Valeria Galindo Marin",
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            IdempotencyKey = "bson-test-key",
            Items =
            [
                new OrderItem
                {
                    ProductId = productId,
                    ProductName = "Keyboard",
                    Quantity = 2,
                    UnitPrice = 25.50m,
                    LineTotal = 51.00m
                }
            ],
            Subtotal = 51.00m,
            Tax = 9.18m,
            Total = 60.18m
        };

        var document = order.ToBsonDocument();
        var item = document["Items"].AsBsonArray[0].AsBsonDocument;

        Assert.True(document.Contains("Items"));
        Assert.Equal("Valeria Galindo Marin", document["CustomerName"].AsString);
        Assert.Equal(productId, item["ProductId"].AsGuid);
        Assert.Equal("Keyboard", item["ProductName"].AsString);
        Assert.Equal(2, item["Quantity"].AsInt32);
    }
}
