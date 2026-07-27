using BuildingBlocks;
using Catalog.API.Models;
using Catalog.API.Models.Products.GetProductById;
using Marten;
using Microsoft.Extensions.Logging;
using Moq;

namespace Catalog.API.Tests;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        var session = new Mock<IDocumentSession>();
        session
            .Setup(s => s.LoadAsync<Product>(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var handler = new GetProductByIdQueryHandler(
            session.Object,
            Mock.Of<ILogger<GetProductByIdQueryHandler>>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None));
    }
}
