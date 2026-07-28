using Basket.Basket.StoreBasket;
using Basket.Models;

namespace Basket.API.Tests;

public class StoreBasketCommandValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenUserIdIsMissing()
    {
        var validator = new StoreBasketCommandValidator();
        var command = new StoreBasketCommand("", new ShoppingCart { UserName = "ignored", Items = [] });

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(StoreBasketCommand.UserId));
    }
}
