namespace Basket.Basket.StoreBasket;

public class StoreBasketCommandValidator : AbstractValidator<StoreBasketCommand>
{
    public StoreBasketCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.Cart)
            .NotNull()
            .DependentRules(() =>
            {
                RuleFor(command => command.Cart.Items)
                    .NotNull();

                RuleForEach(command => command.Cart.Items)
                    .ChildRules(item =>
                    {
                        item.RuleFor(cartItem => cartItem.Quantity)
                            .GreaterThan(0);

                        item.RuleFor(cartItem => cartItem.Price)
                            .GreaterThanOrEqualTo(0);

                        item.RuleFor(cartItem => cartItem.ProductId)
                            .NotEqual(Guid.Empty);

                        item.RuleFor(cartItem => cartItem.ProductName)
                            .NotEmpty();
                    });
            });
    }
}
