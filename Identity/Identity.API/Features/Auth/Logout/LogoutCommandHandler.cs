namespace Identity.API.Features.Auth.Logout;

public class LogoutCommandHandler(IdentityDbContext dbContext, ITokenService tokenService)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var refreshTokenHash = tokenService.HashRefreshToken(command.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
