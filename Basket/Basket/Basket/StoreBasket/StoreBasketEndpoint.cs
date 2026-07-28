namespace Basket.Basket.StoreBasket;

public record StoreBasketRequest(ShoppingCart Cart);

public record StoreBasketResponse(ShoppingCart Cart);

public class StoreBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/basket", async (
            StoreBasketRequest request,
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
            var result = await sender.Send(new StoreBasketCommand(userId, request.Cart), cancellationToken);
            var response = new StoreBasketResponse(result.Cart);

            return Results.Ok(response);
        })
        .RequireAuthorization("ClienteOnly")
        .WithName("StoreBasket")
        .Produces<StoreBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithSummary("Store shopping cart")
        .WithDescription("Creates or updates the shopping cart for the authenticated user.");
    }
}
