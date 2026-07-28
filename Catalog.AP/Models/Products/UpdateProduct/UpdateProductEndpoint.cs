namespace Catalog.API.Models.Products.UpdateProduct
{
    public record UpdateProductRequest(
        string Name,
        string Description,
        List<string> Category,
        string ImageFiles,
        decimal Price);

    public record UpdateProductResponse(bool IsSuccess);

    public class UpdateProductEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("/products/{id}", async (
                Guid id,
                UpdateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateProductCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.Category,
                    request.ImageFiles,
                    request.Price);

                var result = await sender.Send(command, cancellationToken);
                return Results.Ok(result.Adapt<UpdateProductResponse>());
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateProduct")
            .Produces<UpdateProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Update product")
            .WithDescription("Updates a product by identifier.");
        }
    }
}
