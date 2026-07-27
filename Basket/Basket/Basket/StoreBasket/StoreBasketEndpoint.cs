namespace Basket.Basket.StoreBasket;

public record StoreBasketRequest(ShoppingCart Cart);

public record StoreBasketResponse(ShoppingCart Cart);

public class StoreBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/basket", async (
            StoreBasketRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new StoreBasketCommand(request.Cart), cancellationToken);
            var response = new StoreBasketResponse(result.Cart);

            return Results.Ok(response);
        })
        .WithName("StoreBasket")
        .Produces<StoreBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Store shopping cart")
        .WithDescription("Creates or updates the shopping cart for a user.");
    }
}
