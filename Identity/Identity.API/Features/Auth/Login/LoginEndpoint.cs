using Microsoft.Extensions.Options;

namespace Identity.API.Features.Auth.Login;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User);

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            ISender sender,
            ITokenService tokenService,
            IOptions<JwtOptions> jwtOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LoginCommand(
                request.Email,
                request.Password,
                httpContext.Connection.RemoteIpAddress?.ToString()), cancellationToken);

            tokenService.AppendRefreshTokenCookie(httpContext, result.RefreshToken, DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays));

            return Results.Ok(new LoginResponse(result.AccessToken, result.ExpiresAtUtc, result.User));
        })
        .RequireRateLimiting("LoginPolicy")
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
