namespace Orders.API.Clients;

public sealed record BasketResponse(ShoppingCartDto Cart);

public sealed record DeleteBasketResponse(bool IsSuccess);

public sealed class ShoppingCartDto
{
    public string UserName { get; set; } = default!;
    public List<ShoppingCartItemDto> Items { get; set; } = [];
    public decimal TotalPrice { get; set; }
}

public sealed class ShoppingCartItemDto
{
    public int Quantity { get; set; }
    public string Color { get; set; } = default!;
    public decimal Price { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
}
