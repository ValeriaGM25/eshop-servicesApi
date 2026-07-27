namespace Catalog.API.Models.Products.DeleteProduct
{
    public record DeleteProductResponse(bool IsSuccess);

    public class DeleteProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("/products/{id}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteProductCommand(id), cancellationToken);
                var response = result.Adapt<DeleteProductResponse>();
                return Results.Ok(response);
            })
                .WithName("DeleteProduct")
                .Produces<DeleteProductResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Delete product")
                .WithDescription("Deletes a product by identifier.");

        }
    }
}
