namespace Basket.Basket.GetBasket;

public class GetBasketQueryHandler(IBasketRepository basketRepository)
    : IQueryHandler<GetBasketQuery, GetBasketResult>
{
    public async Task<GetBasketResult> Handle(
        GetBasketQuery query,
        CancellationToken cancellationToken)
    {
        var basket = await basketRepository.GetBasket(query.UserName, cancellationToken);

        return new GetBasketResult(basket);
    }
}
