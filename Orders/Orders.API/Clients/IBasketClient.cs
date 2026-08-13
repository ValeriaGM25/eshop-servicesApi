namespace Orders.API.Clients;

public interface IBasketClient
{
    Task<ShoppingCartDto> GetBasketAsync(string bearerToken, CancellationToken cancellationToken);
    Task<bool> DeleteBasketAsync(string bearerToken, CancellationToken cancellationToken);
}
