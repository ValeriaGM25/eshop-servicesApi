namespace Catalog.API.Models.Products.CreateProduct
{
    public record CreateProductRequest(string Name, string Description,
        List<string> Category, string ImageFiles, decimal Price);

    public record CreateProductResponse(Guid Id);

    public class CreateProductEndpoint: ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/products", async (
                CreateProductRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = request.Adapt<CreateProductCommand>();

                var result = await sender.Send(command, cancellationToken);
                var response = result.Adapt<CreateProductResponse>();
                return Results.Created($"/products/{response.Id}", response);
            })
                .RequireAuthorization("AdminOnly")
                .WithName("CreateProduct")
                .Produces<CreateProductResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Create product")
                .WithDescription("Creates a product and returns its identifier.");
        }
    }
}
