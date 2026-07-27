using Basket.Data;
using Basket.Exceptions;
using Basket.Models;
using Marten;
using Moq;

namespace Basket.API.Tests;

public class BasketRepositoryTests
{
    [Fact]
    public async Task GetBasket_ThrowsBasketNotFoundException_WhenCartDoesNotExist()
    {
        var session = new Mock<IDocumentSession>();
        session
            .Setup(s => s.LoadAsync<ShoppingCart>("missing-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);

        var repository = new BasketRepository(session.Object);

        await Assert.ThrowsAsync<BasketNotFoundException>(() =>
            repository.GetBasket("missing-user", CancellationToken.None));
    }
}
