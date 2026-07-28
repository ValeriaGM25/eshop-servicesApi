namespace Basket.Basket.DeleteBasket;

public record DeleteBasketResponse(bool IsSuccess);

public class DeleteBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/basket", async (
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
            var result = await sender.Send(new DeleteBasketCommand(userId), cancellationToken);
            var response = new DeleteBasketResponse(result.IsSuccess);

            return Results.Ok(response);
        })
        .RequireAuthorization("ClienteOnly")
        .WithName("DeleteBasket")
        .Produces<DeleteBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete shopping cart")
        .WithDescription("Deletes the shopping cart for the authenticated user.");
    }
}
