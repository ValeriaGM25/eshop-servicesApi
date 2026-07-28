namespace Identity.API.Features.Auth.GetCurrentUser;

public record GetCurrentUserResponse(AuthUserDto User);

public class GetCurrentUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", async (
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            var result = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
            return Results.Ok(new GetCurrentUserResponse(result.User));
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
