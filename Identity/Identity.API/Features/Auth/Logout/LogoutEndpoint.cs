namespace Identity.API.Features.Auth.Logout;

public class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", async (
            ISender sender,
            ITokenService tokenService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies[TokenService.RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await sender.Send(new LogoutCommand(refreshToken), cancellationToken);
            }

            tokenService.DeleteRefreshTokenCookie(httpContext);
            return Results.NoContent();
        })
        .WithName("Logout")
        .Produces(StatusCodes.Status204NoContent);
    }
}
