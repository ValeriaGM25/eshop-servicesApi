using Basket.Exceptions;

namespace Basket.Data;

public class BasketRepository(IDocumentSession documentSession) : IBasketRepository
{
    public async Task<ShoppingCart> GetBasket(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var basket = await documentSession.LoadAsync<ShoppingCart>(userName, cancellationToken);

        return basket is null
            ? throw new BasketNotFoundException(userName)
            : basket;
    }

    public async Task<ShoppingCart> StoreBasket(
        ShoppingCart basket,
        CancellationToken cancellationToken = default)
    {
        documentSession.Store(basket);
        await documentSession.SaveChangesAsync(cancellationToken);

        return basket;
    }

    public async Task<bool> DeleteBasket(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var basket = await documentSession.LoadAsync<ShoppingCart>(userName, cancellationToken);

        if (basket is null)
        {
            throw new BasketNotFoundException(userName);
        }

        documentSession.Delete<ShoppingCart>(userName);
        await documentSession.SaveChangesAsync(cancellationToken);

        return true;
    }
}
