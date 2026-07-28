namespace Identity.API.Services;

public interface ITokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    void AppendRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresAtUtc);
    void DeleteRefreshTokenCookie(HttpContext context);
}
