namespace Basket.Basket.GetBasket;

public record GetBasketResponse(ShoppingCart Cart);

public class GetBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/basket", async (
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
            var result = await sender.Send(new GetBasketQuery(userId), cancellationToken);
            var response = new GetBasketResponse(result.Cart);

            return Results.Ok(response);
        })
        .RequireAuthorization("ClienteOnly")
        .WithName("GetBasket")
        .Produces<GetBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get shopping cart")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithDescription("Gets the shopping cart for the authenticated user.");
    }
}
