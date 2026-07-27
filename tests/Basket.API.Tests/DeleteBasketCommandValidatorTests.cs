using Basket.Basket.DeleteBasket;

namespace Basket.API.Tests;

public class DeleteBasketCommandValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenUserNameIsMissing()
    {
        var validator = new DeleteBasketCommandValidator();

        var result = validator.Validate(new DeleteBasketCommand(""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(DeleteBasketCommand.UserName));
    }
}
