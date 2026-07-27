using Catalog.API.Models;
using Catalog.API.Models.Products.GetProducts;

namespace Catalog.API.Tests;

public class GetProductsQueryHandlerTests
{
    [Fact]
    public void CreateResult_ReturnsExpectedPaginationShape()
    {
        var query = new GetProductsQuery(1, 10);
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Laptop Pro 14", Description = "Demo", Category = ["Computadoras"], ImageFiles = "placeholder", Price = 10 }
        };

        var result = GetProductsQueryHandler.CreateResult(query, products, 8);

        Assert.Equal(1, result.Products.PageNumber);
        Assert.Equal(10, result.Products.PageSize);
        Assert.Equal(8, result.Products.TotalCount);
        Assert.Single(result.Products.Data);
    }
}
