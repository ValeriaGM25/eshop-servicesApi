namespace Basket.Basket.DeleteBasket;

public class DeleteBasketCommandHandler(IBasketRepository basketRepository)
    : ICommandHandler<DeleteBasketCommand, DeleteBasketResult>
{
    public async Task<DeleteBasketResult> Handle(
        DeleteBasketCommand command,
        CancellationToken cancellationToken)
    {
        var isSuccess = await basketRepository.DeleteBasket(command.UserName, cancellationToken);

        return new DeleteBasketResult(isSuccess);
    }
}
