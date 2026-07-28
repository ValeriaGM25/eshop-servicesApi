using Basket.Basket.StoreBasket;
using Basket.Data;
using Basket.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;

namespace Basket.API.Tests;

public class AuthorizationAndOwnerTests
{
    [Fact]
    public void ClienteOnlyPolicy_AllowsOnlyClienteRole()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("ClienteOnly", policy => policy.RequireRole("Cliente"));

        var requirement = Assert.Single(options.GetPolicy("ClienteOnly")!.Requirements.OfType<RolesAuthorizationRequirement>());

        Assert.Equal(["Cliente"], requirement.AllowedRoles);
    }

    [Fact]
    public async Task StoreBasket_UsesOwnerFromTokenCommandInsteadOfRequestCart()
    {
        var repository = new Mock<IBasketRepository>();
        repository.Setup(repo => repo.StoreBasket(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart cart, CancellationToken _) => cart);
        var handler = new StoreBasketCommandHandler(repository.Object);
        var cart = new ShoppingCart
        {
            UserName = "attacker-user",
            Items =
            [
                new ShoppingCartItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Temporary",
                    Price = 1,
                    Quantity = 1
                }
            ]
        };

        var result = await handler.Handle(new StoreBasketCommand("token-user", cart), CancellationToken.None);

        Assert.Equal("token-user", result.Cart.UserName);
        repository.Verify(repo => repo.StoreBasket(It.Is<ShoppingCart>(stored => stored.UserName == "token-user"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
