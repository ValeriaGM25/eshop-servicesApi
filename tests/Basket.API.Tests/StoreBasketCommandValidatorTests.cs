using Basket.Basket.StoreBasket;
using Basket.Models;

namespace Basket.API.Tests;

public class StoreBasketCommandValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenCartUserNameIsMissing()
    {
        var validator = new StoreBasketCommandValidator();
        var command = new StoreBasketCommand(new ShoppingCart { UserName = "", Items = [] });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Cart.UserName");
    }
}
