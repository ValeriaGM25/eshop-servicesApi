namespace Catalog.API.Models.Products.GetProductByCategory
{
    //public record GetProductByCategoryRequest()
    public record GetProductByCategoryResponse(IEnumerable<Product> Products);

    public class GetProductCategoryEndPoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products/category/{category}", async (string category, ISender sender) =>
            {
                var result = await sender.Send(new GetProductByCategoryQuery(category));
                var response = result.Adapt<GetProductByCategoryResponse>();
                return Results.Ok(response);
            })
                .WithName("GetProductsCategory")
                .Produces<GetProductByCategoryResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .WithSummary("Obtener Producto por categoria")
                .WithDescription("Obtener producto por categoria");
        }
    }
}
