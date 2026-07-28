using Microsoft.Extensions.Options;

namespace Identity.API.Features.Auth.Refresh;

public class RefreshCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<RefreshCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        var refreshTokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token inválido.");
        }

        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAt,
            CreatedByIp = command.CreatedByIp
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = await tokenService.CreateAccessTokenAsync(storedToken.User, cancellationToken);
        return new AuthResult(accessToken.AccessToken, accessToken.ExpiresAtUtc, accessToken.User, newRefreshToken);
    }
}
