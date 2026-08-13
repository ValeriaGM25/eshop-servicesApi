using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orders.API.Domain;

public sealed class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string CustomerId { get; set; } = default!;
    public string? CustomerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string IdempotencyKey { get; set; } = default!;
}
