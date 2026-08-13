namespace Orders.API.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2
}
