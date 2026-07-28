using Identity.API.Features.Auth.Register;
using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Identity.API.Tests;

public class RegisterTests
{
    [Fact]
    public async Task Register_AssignsClienteRole()
    {
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("cliente@ejemplo.com"))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), "Password1!"))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Cliente))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new RegisterCommandHandler(userManager.Object);

        var result = await handler.Handle(
            new RegisterCommand("Cliente", "cliente@ejemplo.com", "Password1!", "Password1!"),
            CancellationToken.None);

        Assert.Contains(IdentityRoles.Cliente, result.User.Roles);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin), Times.Never);
    }

    [Fact]
    public async Task Register_DoesNotAllowClientToBecomeAdmin()
    {
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Cliente))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new RegisterCommandHandler(userManager.Object);

        await handler.Handle(new RegisterCommand("Admin", "admin@ejemplo.com", "Password1!", "Password1!"), CancellationToken.None);

        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin), Times.Never);
    }

    [Fact]
    public async Task Register_ThrowsConflict_WhenEmailIsDuplicated()
    {
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("cliente@ejemplo.com"))
            .ReturnsAsync(new ApplicationUser { Email = "cliente@ejemplo.com" });

        var handler = new RegisterCommandHandler(userManager.Object);

        await Assert.ThrowsAsync<BuildingBlocks.ConflictException>(() =>
            handler.Handle(new RegisterCommand("Cliente", "cliente@ejemplo.com", "Password1!", "Password1!"), CancellationToken.None));
    }

    [Fact]
    public void RegisterValidator_Fails_WhenPasswordsDoNotMatch()
    {
        var validator = new RegisterCommandValidator();

        var result = validator.Validate(new RegisterCommand("Cliente", "cliente@ejemplo.com", "Password1!", "Other1!"));

        Assert.False(result.IsValid);
    }
}
