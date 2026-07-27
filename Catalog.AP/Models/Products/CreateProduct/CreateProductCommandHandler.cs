using Marten;
using System.Windows.Input;

namespace Catalog.API.Models.Products.CreateProduct
{
    /*record nos permite crear el producto con los datos para registrar coo uno nuevo*/
    public record CreateProductCommand(string Name, string Description,
        List<string> Category, string ImageFiles, decimal Price)
        : ICommand<CreateProductResult>;

    /*este record retorna el objeto de respuesta es decir el identificador del objeto insertar*/
    public record CreateProductResult(Guid Id);

    internal class CreateProductCommandHandler(IDocumentSession documentSession):
        ICommandHandler<CreateProductCommand,
            CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            Product product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                ImageFiles = request.ImageFiles,
                Price = request.Price,

            };
            documentSession.Store(product);
            await documentSession.SaveChangesAsync(cancellationToken);
            return new CreateProductResult(product.Id);
        }
    }
}

