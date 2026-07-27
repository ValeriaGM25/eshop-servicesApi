using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts
{
    public record GetProductsResponse(PaginateResult<Product> Products);


    public class GetProductsEndPoint : ICarterModule
    {

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/products", 
                async (
                    int pageNumber,
                    int pageSize,
                    ISender sender) =>
            {
                var query =
                    new GetProductsQuery(
                        pageNumber,
                        pageSize);
                var result = await sender.Send(query);

                var response =
                result.Adapt<GetProductsResponse>();
                return Results.Ok(response);
            
            })
                .WithName("GetProducs")
                .Produces<GetProductsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Get Resumen")
                .WithDescription("Get Resumen");
        }
    }
}
