using Basket.Models;

namespace Basket.API.Tests;

public class ShoppingCartTests
{
    [Fact]
    public void TotalPrice_ReturnsQuantityMultipliedByPrice()
    {
        var cart = new ShoppingCart
        {
            UserName = "valeria",
            Items =
            [
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Mouse", Price = 20, Quantity = 2 },
                new ShoppingCartItem { ProductId = Guid.NewGuid(), ProductName = "Keyboard", Price = 50, Quantity = 1 }
            ]
        };

        Assert.Equal(90, cart.TotalPrice);
    }
}
