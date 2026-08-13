using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Orders.API.Domain;

public sealed class OrderItem
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
