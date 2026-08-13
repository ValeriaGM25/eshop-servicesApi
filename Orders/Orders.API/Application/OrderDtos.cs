namespace Orders.API.Application;

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderDto(
    string Id,
    string CustomerId,
    string CustomerName,
    DateTime CreatedAt,
    OrderStatus Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal Tax,
    decimal Total);

public sealed record OrderSummaryDto(
    string Id,
    string CustomerId,
    string CustomerName,
    DateTime CreatedAt,
    OrderStatus Status,
    int ItemsCount,
    decimal Subtotal,
    decimal Tax,
    decimal Total);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    long TotalPages);

public sealed record OrderQueryParameters
{
    public OrderStatus? Status { get; init; }
    public string? CustomerId { get; init; }
    public string? Search { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public OrderQueryParameters Normalize()
    {
        return new OrderQueryParameters
        {
            Status = Status,
            CustomerId = string.IsNullOrWhiteSpace(CustomerId) ? null : CustomerId.Trim(),
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            From = From?.ToUniversalTime(),
            To = To?.ToUniversalTime(),
            Page = Page < 1 ? 1 : Page,
            PageSize = PageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => PageSize
            }
        };
    }
}

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.GetCustomerNameDisplay(),
            order.CreatedAt,
            order.Status,
            order.Items.Select(item => new OrderItemDto(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal)).ToList(),
            order.Subtotal,
            order.Tax,
            order.Total);
    }

    public static OrderSummaryDto ToSummaryDto(this Order order)
    {
        return new OrderSummaryDto(
            order.Id,
            order.CustomerId,
            order.GetCustomerNameDisplay(),
            order.CreatedAt,
            order.Status,
            order.Items.Sum(item => item.Quantity),
            order.Subtotal,
            order.Tax,
            order.Total);
    }

    public static string GetCustomerNameDisplay(this Order order)
    {
        return string.IsNullOrWhiteSpace(order.CustomerName) ? "No disponible" : order.CustomerName.Trim();
    }
}
