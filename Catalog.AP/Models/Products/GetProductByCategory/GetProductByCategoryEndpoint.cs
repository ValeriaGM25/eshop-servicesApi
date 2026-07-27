namespace Catalog.API.Models.Products.GetProductByCategory
{
    public record GetProductByCategoryResponse(IEnumerable<Product> Products);

    public class GetProductByCategoryEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products/category/{category}", async (
                string category,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetProductByCategoryQuery(category), cancellationToken);
                var response = result.Adapt<GetProductByCategoryResponse>();
                return Results.Ok(response);
            })
                .WithName("GetProductsByCategory")
                .Produces<GetProductByCategoryResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithSummary("Get products by category")
                .WithDescription("Gets products that match the specified category.");
        }
    }
}
