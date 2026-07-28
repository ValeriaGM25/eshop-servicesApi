using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Identity.API.Tests;

public class IdentitySeederTests
{
    [Fact]
    public async Task SeedAdmin_CreatesAdminWithOnlyAdminRole()
    {
        ApplicationUser? createdUser = null;
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com"))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        Assert.NotNull(createdUser);
        Assert.Equal("admin@gmail.com", createdUser.Email);
        Assert.Equal("admin@gmail.com", createdUser.UserName);
        Assert.Equal("Administrador", createdUser.FullName);
        Assert.True(createdUser.IsActive);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin), Times.Once);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Cliente), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_DoesNotCreateDuplicate_WhenAdminExists()
    {
        var admin = new ApplicationUser { Email = "admin@gmail.com", UserName = "admin@gmail.com", FullName = "Old", IsActive = false };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com"))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.UpdateAsync(admin))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(admin))
            .ReturnsAsync([IdentityRoles.Admin]);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        Assert.Equal("Administrador", admin.FullName);
        Assert.True(admin.IsActive);
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.AddToRoleAsync(admin, IdentityRoles.Admin), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_RemovesIncorrectRoles()
    {
        var admin = new ApplicationUser { Email = "admin@gmail.com", UserName = "admin@gmail.com", FullName = "Administrador", IsActive = true };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com"))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.UpdateAsync(admin))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(admin))
            .ReturnsAsync([IdentityRoles.Admin, IdentityRoles.Cliente, "Other"]);
        userManager.Setup(manager => manager.RemoveFromRolesAsync(admin, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.RemoveFromRolesAsync(
            admin,
            It.Is<IEnumerable<string>>(roles => roles.SequenceEqual(new[] { IdentityRoles.Cliente, "Other" }))), Times.Once);
    }

    [Fact]
    public async Task SeedAdmin_DoesNotResetPassword_WhenForceResetIsFalse()
    {
        var admin = new ApplicationUser { Email = "admin@gmail.com", UserName = "admin@gmail.com", FullName = "Administrador", IsActive = true };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com"))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.UpdateAsync(admin))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(admin))
            .ReturnsAsync([IdentityRoles.Admin]);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_ResetsPassword_WhenForceResetIsTrueInDevelopment()
    {
        var admin = new ApplicationUser { Email = "admin@gmail.com", UserName = "admin@gmail.com", FullName = "Administrador", IsActive = true };
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync("admin@gmail.com"))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.UpdateAsync(admin))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.GetRolesAsync(admin))
            .ReturnsAsync([IdentityRoles.Admin]);
        userManager.Setup(manager => manager.GeneratePasswordResetTokenAsync(admin))
            .ReturnsAsync("reset-token");
        userManager.Setup(manager => manager.ResetPasswordAsync(admin, "reset-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, CreateConfiguration(forceReset: true), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.ResetPasswordAsync(admin, "reset-token", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EnsureRoles_CreatesOnlyAdminAndCliente()
    {
        var roles = new[] { new ApplicationRole { Name = "Legacy" } }.AsQueryable();
        var roleManager = IdentityTestHelpers.MockRoleManager(roles);
        roleManager.Setup(manager => manager.DeleteAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        roleManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.EnsureRolesAsync(roleManager.Object, Mock.Of<ILogger>());

        roleManager.Verify(manager => manager.CreateAsync(It.Is<ApplicationRole>(role => role.Name == IdentityRoles.Admin)), Times.Once);
        roleManager.Verify(manager => manager.CreateAsync(It.Is<ApplicationRole>(role => role.Name == IdentityRoles.Cliente)), Times.Once);
        roleManager.Verify(manager => manager.DeleteAsync(It.Is<ApplicationRole>(role => role.Name == "Legacy")), Times.Once);
    }

    private static IConfiguration CreateConfiguration(bool forceReset)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUTH_ADMIN_EMAIL"] = "ADMIN@gmail.com",
                ["AUTH_ADMIN_PASSWORD"] = "local-password",
                ["AUTH_ADMIN_FULL_NAME"] = "Administrador",
                ["AUTH_ADMIN_FORCE_PASSWORD_RESET"] = forceReset.ToString()
            })
            .Build();
    }

    private static IWebHostEnvironment CreateEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(env => env.EnvironmentName).Returns(Environments.Development);
        return environment.Object;
    }
}
