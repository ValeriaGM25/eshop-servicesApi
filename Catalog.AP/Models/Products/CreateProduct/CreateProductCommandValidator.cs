namespace Catalog.API.Models.Products.CreateProduct
{
    public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
            RuleFor(command => command.Description).NotEmpty();
            RuleFor(command => command.Category).NotEmpty();
            RuleFor(command => command.ImageFiles).NotEmpty();
            RuleFor(command => command.Price).GreaterThan(0);
        }
    }
}
