namespace Basket.Basket.StoreBasket;

public class StoreBasketCommandHandler(IBasketRepository basketRepository)
    : ICommandHandler<StoreBasketCommand, StoreBasketResult>
{
    public async Task<StoreBasketResult> Handle(
        StoreBasketCommand command,
        CancellationToken cancellationToken)
    {
        var basket = await basketRepository.StoreBasket(command.Cart, cancellationToken);

        return new StoreBasketResult(basket);
    }
}
