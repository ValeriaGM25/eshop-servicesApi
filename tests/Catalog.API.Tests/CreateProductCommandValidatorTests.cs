using Catalog.API.Models.Products.CreateProduct;

namespace Catalog.API.Tests;

public class CreateProductCommandValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenNameIsMissing()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("", "Description", ["Category"], "placeholder", 10);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProductCommand.Name));
    }
}
