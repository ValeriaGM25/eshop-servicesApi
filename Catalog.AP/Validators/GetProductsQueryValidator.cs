using Catalog.API.Models.Products.GetProducts;
using FluentValidation;

namespace Catalog.API.Validators
{
    public class GetProductsQueryValidator: AbstractValidator<GetProductsQuery>
    {
        public GetProductsQueryValidator() 
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("El número de página debe ser mayor a cero.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("El tamaño de la página debe de estar entre 1 y 100.");
                }
                
    }
}
