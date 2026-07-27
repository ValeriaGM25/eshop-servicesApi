namespace Basket.Basket.GetBasket;

public record GetBasketResponse(ShoppingCart Cart);

public class GetBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/basket/{userName}", async (
            string userName,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetBasketQuery(userName), cancellationToken);
            var response = new GetBasketResponse(result.Cart);

            return Results.Ok(response);
        })
        .WithName("GetBasket")
        .Produces<GetBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get shopping cart")
        .WithDescription("Gets the shopping cart for the specified user name.");
    }
}
