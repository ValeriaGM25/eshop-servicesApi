namespace Orders.API.Application.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128)
            .Matches("^[A-Za-z0-9._:-]+$")
            .WithMessage("Idempotency-Key must contain only letters, numbers, dot, underscore, colon or hyphen.");
        RuleFor(command => command.BearerToken).NotEmpty();
    }
}
