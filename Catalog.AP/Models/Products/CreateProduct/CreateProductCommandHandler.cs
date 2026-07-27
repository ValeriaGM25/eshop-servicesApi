namespace Catalog.API.Models.Products.CreateProduct
{
    public record CreateProductCommand(string Name, string Description,
        List<string> Category, string ImageFiles, decimal Price)
        : ICommand<CreateProductResult>;

    public record CreateProductResult(Guid Id);

    public class CreateProductCommandHandler(IDocumentSession documentSession):
        ICommandHandler<CreateProductCommand,
            CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            Product product = new Product
            {
                Id = Guid.NewGuid(),
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

