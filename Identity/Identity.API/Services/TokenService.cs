using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Identity.API.Services;

public class TokenService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment environment) : ITokenService
{
    public const string RefreshTokenCookieName = "eshop_refresh_token";
    private readonly JwtOptions options = jwtOptions.Value;

    public async Task<AccessTokenResult> CreateAccessTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            Expires = expiresAt,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var accessToken = handler.CreateToken(descriptor);

        return new AccessTokenResult(
            accessToken,
            expiresAt,
            new AuthUserDto(user.Id, user.FullName, user.Email!, roles.ToArray()));
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    public void AppendRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresAtUtc)
    {
        context.Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/auth",
                Expires = expiresAtUtc
            });
    }

    public void DeleteRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/auth"
            });
    }
}
