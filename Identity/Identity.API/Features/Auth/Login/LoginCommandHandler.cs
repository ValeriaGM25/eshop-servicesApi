using Microsoft.Extensions.Options;

namespace Identity.API.Features.Auth.Login;

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        var accessToken = await tokenService.CreateAccessTokenAsync(user, cancellationToken);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt,
            CreatedByIp = command.CreatedByIp
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken.AccessToken, accessToken.ExpiresAtUtc, accessToken.User, refreshToken);
    }
}
