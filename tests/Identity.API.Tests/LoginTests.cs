using Identity.API.Data;
using Identity.API.Features.Auth.Login;
using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.API.Tests;

public class LoginTests
{
    [Fact]
    public async Task Login_Succeeds_WhenCredentialsAreValid()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser { Id = "user-1", Email = "cliente@ejemplo.com", FullName = "Cliente", IsActive = true };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("cliente@ejemplo.com")).ReturnsAsync(user);
        var signInManager = IdentityTestHelpers.MockSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.CheckPasswordSignInAsync(user, "Password1!", true)).ReturnsAsync(SignInResult.Success);
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateAccessTokenAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15), new AuthUserDto(user.Id, user.FullName, user.Email!, [IdentityRoles.Cliente])));
        tokenService.Setup(service => service.GenerateRefreshToken()).Returns("refresh-token");
        tokenService.Setup(service => service.HashRefreshToken("refresh-token")).Returns("refresh-token-hash");

        var handler = new LoginCommandHandler(userManager.Object, signInManager.Object, dbContext, tokenService.Object, Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

        var result = await handler.Handle(new LoginCommand("cliente@ejemplo.com", "Password1!", "127.0.0.1"), CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token-hash", (await dbContext.RefreshTokens.SingleAsync()).TokenHash);
    }

    [Fact]
    public async Task Login_Fails_WhenCredentialsAreInvalid()
    {
        await using var dbContext = CreateDbContext();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("missing@ejemplo.com")).ReturnsAsync((ApplicationUser?)null);
        var signInManager = IdentityTestHelpers.MockSignInManager(userManager.Object);
        var handler = new LoginCommandHandler(userManager.Object, signInManager.Object, dbContext, Mock.Of<ITokenService>(), Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("missing@ejemplo.com", "Password1!", "127.0.0.1"), CancellationToken.None));

        Assert.Equal("Credenciales inválidas.", exception.Message);
    }

    [Fact]
    public async Task Login_Succeeds_ForAdminAndIncludesAdminRole()
    {
        await using var dbContext = CreateDbContext();
        var user = new ApplicationUser { Id = "admin-1", Email = "admin@gmail.com", FullName = "Administrador", IsActive = true };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com")).ReturnsAsync(user);
        var signInManager = IdentityTestHelpers.MockSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.CheckPasswordSignInAsync(user, It.IsAny<string>(), true)).ReturnsAsync(SignInResult.Success);
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateAccessTokenAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(15), new AuthUserDto(user.Id, user.FullName, user.Email!, [IdentityRoles.Admin])));
        tokenService.Setup(service => service.GenerateRefreshToken()).Returns("refresh-token");
        tokenService.Setup(service => service.HashRefreshToken("refresh-token")).Returns("refresh-token-hash");

        var handler = new LoginCommandHandler(userManager.Object, signInManager.Object, dbContext, tokenService.Object, Options.Create(new JwtOptions { RefreshTokenDays = 7 }));

        var result = await handler.Handle(new LoginCommand("admin@gmail.com", "local-password", "127.0.0.1"), CancellationToken.None);

        Assert.Equal("admin@gmail.com", result.User.Email);
        Assert.Equal("Administrador", result.User.FullName);
        Assert.Contains(IdentityRoles.Admin, result.User.Roles);
    }

    private static IdentityDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
