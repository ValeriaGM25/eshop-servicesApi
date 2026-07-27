using Catalog.API.Models.Products.GetProducts;
using Catalog.API.Validators;

namespace Catalog.API.Tests;

public class GetProductsQueryValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenPageNumberIsInvalid()
    {
        var validator = new GetProductsQueryValidator();

        var result = validator.Validate(new GetProductsQuery(0, 10));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetProductsQuery.PageNumber));
    }

    [Fact]
    public void Validate_ReturnsError_WhenPageSizeIsInvalid()
    {
        var validator = new GetProductsQueryValidator();

        var result = validator.Validate(new GetProductsQuery(1, 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetProductsQuery.PageSize));
    }
}
