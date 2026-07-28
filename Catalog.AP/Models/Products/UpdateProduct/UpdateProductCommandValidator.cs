namespace Catalog.API.Models.Products.UpdateProduct
{
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(command => command.Id).NotEqual(Guid.Empty);
            RuleFor(command => command.Name).NotEmpty();
            RuleFor(command => command.Description).NotEmpty();
            RuleFor(command => command.Category).NotEmpty();
            RuleFor(command => command.ImageFiles).NotEmpty();
            RuleFor(command => command.Price).GreaterThan(0);
        }
    }
}
