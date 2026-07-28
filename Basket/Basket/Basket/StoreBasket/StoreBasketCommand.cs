namespace Basket.Basket.StoreBasket;

public record StoreBasketCommand(string UserId, ShoppingCart Cart) : ICommand<StoreBasketResult>;

public record StoreBasketResult(ShoppingCart Cart);
