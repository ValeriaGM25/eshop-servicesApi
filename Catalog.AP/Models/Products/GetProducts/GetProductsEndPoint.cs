using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts
{
    public record GetProductsResponse(PaginateResult<Product> Products);

    public class GetProductsEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products",
                async (
                    int? pageNumber,
                    int? pageSize,
                    ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var query = new GetProductsQuery(
                        pageNumber ?? 1,
                        pageSize ?? 10);
                    var result = await sender.Send(query, cancellationToken);

                    var response = result.Adapt<GetProductsResponse>();
                    return Results.Ok(response);
                })
                .WithName("GetProducts")
                .Produces<GetProductsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Get products")
                .WithDescription("Gets a paginated product list.");
        }
    }
}
