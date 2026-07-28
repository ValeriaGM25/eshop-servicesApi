namespace Catalog.API.Models.Products.UpdateProduct
{
    public record UpdateProductCommand(
        Guid Id,
        string Name,
        string Description,
        List<string> Category,
        string ImageFiles,
        decimal Price) : ICommand<UpdateProductResult>;

    public record UpdateProductResult(bool IsSuccess);

    public class UpdateProductCommandHandler(IDocumentSession session)
        : ICommandHandler<UpdateProductCommand, UpdateProductResult>
    {
        public async Task<UpdateProductResult> Handle(
            UpdateProductCommand command,
            CancellationToken cancellationToken)
        {
            var product = await session.LoadAsync<Product>(command.Id, cancellationToken);

            if (product is null)
            {
                throw new NotFoundException(nameof(Product), command.Id);
            }

            product.Name = command.Name;
            product.Description = command.Description;
            product.Category = command.Category;
            product.ImageFiles = command.ImageFiles;
            product.Price = command.Price;

            session.Store(product);
            await session.SaveChangesAsync(cancellationToken);

            return new UpdateProductResult(true);
        }
    }
}
