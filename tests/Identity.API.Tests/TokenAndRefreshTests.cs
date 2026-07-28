using Identity.API.Data;
using Identity.API.Features.Auth.Refresh;
using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace Identity.API.Tests;

public class TokenAndRefreshTests
{
    [Fact]
    public void RefreshTokenHash_DoesNotStoreRawToken()
    {
        var service = CreateTokenService([IdentityRoles.Cliente]);

        var rawToken = "raw-refresh-token";
        var hash = service.HashRefreshToken(rawToken);

        Assert.NotEqual(rawToken, hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public async Task AccessToken_ContainsSubAndRoleClaims()
    {
        var service = CreateTokenService([IdentityRoles.Cliente]);

        var result = await service.CreateAccessTokenAsync(new ApplicationUser
        {
            Id = "user-1",
            Email = "cliente@ejemplo.com",
            FullName = "Cliente"
        }, CancellationToken.None);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(result.AccessToken);

        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "user-1");
        Assert.Contains(IdentityRoles.Cliente, result.User.Roles);
    }

    [Fact]
    public async Task Refresh_RotatesToken()
    {
        await using var dbContext = CreateDbContext();
        var tokenService = new FakeTokenService();
        var user = new ApplicationUser { Id = "user-1", Email = "cliente@ejemplo.com", FullName = "Cliente", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken("old-token"),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        await dbContext.SaveChangesAsync();

        var handler = new RefreshCommandHandler(dbContext, tokenService, Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

        await handler.Handle(new RefreshCommand("old-token", "127.0.0.1"), CancellationToken.None);

        var oldToken = await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenService.HashRefreshToken("old-token"));
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(2, await dbContext.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RevokedToken_CannotBeReused()
    {
        await using var dbContext = CreateDbContext();
        var tokenService = new FakeTokenService();
        var user = new ApplicationUser { Id = "user-1", Email = "cliente@ejemplo.com", FullName = "Cliente", IsActive = true };
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken("old-token"),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var handler = new RefreshCommandHandler(dbContext, tokenService, Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshCommand("old-token", "127.0.0.1"), CancellationToken.None));
    }

    [Fact]
    public void SeederRoles_AreOnlyAdminAndCliente()
    {
        Assert.Equal([IdentityRoles.Admin, IdentityRoles.Cliente], IdentityRoles.All);
    }

    private static TokenService CreateTokenService(IReadOnlyList<string> roles)
    {
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(roles.ToArray());

        return new TokenService(
            userManager.Object,
            Options.Create(new JwtOptions
            {
                Issuer = "eshop.identity",
                Audience = "eshop.apis",
                Key = "this-is-a-long-test-key-for-jwt-signing-only",
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7
            }),
            Mock.Of<IWebHostEnvironment>());
    }

    private static IdentityDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class FakeTokenService : ITokenService
    {
        public Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15), new AuthUserDto(user.Id, user.FullName, user.Email!, [IdentityRoles.Cliente])));

        public string GenerateRefreshToken() => Guid.NewGuid().ToString();

        public string HashRefreshToken(string refreshToken) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(refreshToken)));

        public void AppendRefreshTokenCookie(HttpContext context, string refreshToken, DateTime expiresAtUtc)
        {
        }

        public void DeleteRefreshTokenCookie(HttpContext context)
        {
        }
    }
}
