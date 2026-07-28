using Microsoft.Extensions.Options;

namespace Identity.API.Features.Auth.Refresh;

public record RefreshResponse(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User);

public class RefreshEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (
            ISender sender,
            ITokenService tokenService,
            IOptions<JwtOptions> jwtOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var refreshToken = httpContext.Request.Cookies[TokenService.RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedAccessException("Refresh token inválido.");
            }

            var result = await sender.Send(new RefreshCommand(
                refreshToken,
                httpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

            tokenService.AppendRefreshTokenCookie(httpContext, result.RefreshToken, DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays));

            return Results.Ok(new RefreshResponse(result.AccessToken, result.ExpiresAtUtc, result.User));
        })
        .WithName("Refresh")
        .Produces<RefreshResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
