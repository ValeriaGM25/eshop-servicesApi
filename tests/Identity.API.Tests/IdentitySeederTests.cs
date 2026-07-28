using Identity.API.Data;
using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Identity.API.Tests;

public class IdentitySeederTests
{
    private const string AdminEmail = "admin@gmail.com";
    private const string TestPassword = "Unit_Test_Admin_1!";
    private const string NewTestPassword = "Unit_Test_Admin_2!";

    [Fact]
    public async Task SeedAdmin_CreatesAdminWithOnlyAdminRole()
    {
        await using var dbContext = CreateDbContext();
        ApplicationUser? createdUser = null;
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        Assert.NotNull(createdUser);
        Assert.Equal(AdminEmail, createdUser.Email);
        Assert.Equal(AdminEmail, createdUser.UserName);
        Assert.Equal("Administrator", createdUser.FullName);
        Assert.True(createdUser.IsActive);
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), TestPassword), Times.Once);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Admin), Times.Once);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), IdentityRoles.Cliente), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_DoesNotCreateDuplicate_WhenAdminExists()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_DoesNotModifyExistingAdmin_WhenForceResetIsFalse()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin(fullName: "Existing Name", isActive: false);
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        Assert.Equal("Existing Name", admin.FullName);
        Assert.False(admin.IsActive);
        userManager.Verify(manager => manager.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_ForceFalse_IsIdempotent()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());
        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        userManager.Verify(manager => manager.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_ResetsPassword_WhenForceResetIsTrue()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        SetupSuccessfulPasswordReset(userManager, admin);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true, password: NewTestPassword), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.ResetPasswordAsync(admin, "reset-token", NewTestPassword), Times.Once);
        userManager.Verify(manager => manager.UpdateSecurityStampAsync(admin), Times.Once);
    }

    [Fact]
    public async Task SeedAdmin_ForceTrue_PreservesAdminIdAndRole()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin(id: "stable-admin-id");
        var userManager = IdentityTestHelpers.MockUserManager();
        SetupSuccessfulPasswordReset(userManager, admin);
        userManager.Setup(manager => manager.IsInRoleAsync(admin, IdentityRoles.Admin))
            .ReturnsAsync(true);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true), CreateEnvironment(), Mock.Of<ILogger>());

        Assert.Equal("stable-admin-id", admin.Id);
        userManager.Verify(manager => manager.AddToRoleAsync(admin, IdentityRoles.Admin), Times.Never);
        userManager.Verify(manager => manager.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task SeedAdmin_ForceTrue_AssignsAdminRole_WhenMissing()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        SetupSuccessfulPasswordReset(userManager, admin);
        userManager.Setup(manager => manager.IsInRoleAsync(admin, IdentityRoles.Admin))
            .ReturnsAsync(false);
        userManager.Setup(manager => manager.AddToRoleAsync(admin, IdentityRoles.Admin))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true), CreateEnvironment(), Mock.Of<ILogger>());

        userManager.Verify(manager => manager.AddToRoleAsync(admin, IdentityRoles.Admin), Times.Once);
    }

    [Fact]
    public async Task SeedAdmin_ForceTrue_RevokesActiveRefreshTokens()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin(id: "admin-with-tokens");
        dbContext.RefreshTokens.AddRange(
            ActiveRefreshToken(admin.Id),
            ActiveRefreshToken(admin.Id),
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                TokenHash = "revoked-token-hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
                RevokedAtUtc = DateTime.UtcNow.AddHours(-1)
            });
        await dbContext.SaveChangesAsync();

        var userManager = IdentityTestHelpers.MockUserManager();
        SetupSuccessfulPasswordReset(userManager, admin);

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true), CreateEnvironment(), Mock.Of<ILogger>());

        var tokens = await dbContext.RefreshTokens.Where(token => token.UserId == admin.Id).ToListAsync();
        Assert.All(tokens, token => Assert.True(token.RevokedAtUtc.HasValue));
    }

    [Fact]
    public async Task SeedAdmin_ForceTrue_ChangesPasswordAndPreservesUserIdentity()
    {
        await using var serviceProvider = CreateIdentityServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await roleManager.CreateAsync(new ApplicationRole { Name = IdentityRoles.Admin });

        var admin = ExistingAdmin(id: "persistent-admin-id");
        var createResult = await userManager.CreateAsync(admin, TestPassword);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Code)));
        await userManager.AddToRoleAsync(admin, IdentityRoles.Admin);
        dbContext.RefreshTokens.Add(ActiveRefreshToken(admin.Id));
        await dbContext.SaveChangesAsync();

        await IdentitySeeder.SeedAdminAsync(userManager, dbContext, CreateConfiguration(forceReset: true, password: NewTestPassword), CreateEnvironment(), Mock.Of<ILogger>());

        var persistedAdmin = (await userManager.FindByEmailAsync(AdminEmail))!;
        Assert.Equal("persistent-admin-id", persistedAdmin.Id);
        Assert.False(await userManager.CheckPasswordAsync(persistedAdmin, TestPassword));
        Assert.True(await userManager.CheckPasswordAsync(persistedAdmin, NewTestPassword));
        Assert.True(await userManager.IsInRoleAsync(persistedAdmin, IdentityRoles.Admin));
        Assert.All(await dbContext.RefreshTokens.Where(token => token.UserId == persistedAdmin.Id).ToListAsync(), token => Assert.True(token.RevokedAtUtc.HasValue));
    }

    [Fact]
    public async Task SeedAdmin_EmptyPasswordWithForceTrue_ThrowsClearError()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true, password: ""), CreateEnvironment(), Mock.Of<ILogger>()));

        Assert.Equal("AUTH_ADMIN_PASSWORD is required when password reset is enabled.", exception.Message);
    }

    [Fact]
    public async Task SeedAdmin_InvalidPassword_ReturnsIdentityCodesWithoutSecret()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.IsInRoleAsync(admin, IdentityRoles.Admin))
            .ReturnsAsync(true);
        userManager.Setup(manager => manager.GeneratePasswordResetTokenAsync(admin))
            .ReturnsAsync("reset-token");
        userManager.Setup(manager => manager.ResetPasswordAsync(admin, "reset-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordRequiresDigit" }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true, password: "invalid-test-value"), CreateEnvironment(), Mock.Of<ILogger>()));

        Assert.Equal("Could not reset initial admin password: PasswordRequiresDigit", exception.Message);
        Assert.DoesNotContain("invalid-test-value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAdmin_MissingPasswordForCreate_ThrowsClearError()
    {
        await using var dbContext = CreateDbContext();
        var userManager = IdentityTestHelpers.MockUserManager();
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync((ApplicationUser?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: false, password: ""), CreateEnvironment(), Mock.Of<ILogger>()));

        Assert.Equal("AUTH_ADMIN_PASSWORD is required to create the initial administrator.", exception.Message);
    }

    [Fact]
    public void AdminBootstrapOptions_ForcePasswordReset_ParsesTrueAndFalse()
    {
        Assert.True(AdminBootstrapOptions.FromConfiguration(CreateConfiguration(forceReset: true)).ForcePasswordReset);
        Assert.False(AdminBootstrapOptions.FromConfiguration(CreateConfiguration(forceReset: false)).ForcePasswordReset);
    }

    [Fact]
    public void AdminBootstrapOptions_ProductionRequiresEmail()
    {
        var options = AdminBootstrapOptions.FromConfiguration(new ConfigurationBuilder().Build());

        var exception = Assert.Throws<InvalidOperationException>(() => options.ValidateForProduction(CreateEnvironment(Environments.Production)));

        Assert.Equal("AUTH_ADMIN_EMAIL is not configured.", exception.Message);
    }

    [Fact]
    public void IdentitySeeder_DoesNotAssignPasswordHashDirectly()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Identity", "Identity.API", "Services", "IdentitySeeder.cs"));

        Assert.DoesNotContain("PasswordHash =", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAdmin_LogsDoNotContainPasswordResetTokenOrPasswordHash()
    {
        await using var dbContext = CreateDbContext();
        var admin = ExistingAdmin();
        var userManager = IdentityTestHelpers.MockUserManager();
        SetupSuccessfulPasswordReset(userManager, admin);
        var logger = new CapturingLogger();

        await IdentitySeeder.SeedAdminAsync(userManager.Object, dbContext, CreateConfiguration(forceReset: true, password: NewTestPassword), CreateEnvironment(), logger);

        var logs = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain(NewTestPassword, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("reset-token", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureRoles_CreatesOnlyAdminAndCliente()
    {
        var roles = Array.Empty<ApplicationRole>().AsQueryable();
        var roleManager = IdentityTestHelpers.MockRoleManager(roles);
        roleManager.Setup(manager => manager.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        roleManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        await IdentitySeeder.EnsureRolesAsync(roleManager.Object, Mock.Of<ILogger>());

        roleManager.Verify(manager => manager.CreateAsync(It.Is<ApplicationRole>(role => role.Name == IdentityRoles.Admin)), Times.Once);
        roleManager.Verify(manager => manager.CreateAsync(It.Is<ApplicationRole>(role => role.Name == IdentityRoles.Cliente)), Times.Once);
    }

    private static void SetupSuccessfulPasswordReset(Mock<UserManager<ApplicationUser>> userManager, ApplicationUser admin)
    {
        userManager.Setup(manager => manager.FindByEmailAsync(AdminEmail))
            .ReturnsAsync(admin);
        userManager.Setup(manager => manager.IsInRoleAsync(admin, IdentityRoles.Admin))
            .ReturnsAsync(true);
        userManager.Setup(manager => manager.GeneratePasswordResetTokenAsync(admin))
            .ReturnsAsync("reset-token");
        userManager.Setup(manager => manager.ResetPasswordAsync(admin, "reset-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(manager => manager.UpdateSecurityStampAsync(admin))
            .ReturnsAsync(IdentityResult.Success);
    }

    private static ApplicationUser ExistingAdmin(string id = "admin-id", string fullName = "Administrator", bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = AdminEmail,
            UserName = AdminEmail,
            FullName = fullName,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = isActive
        };
    }

    private static RefreshToken ActiveRefreshToken(string userId)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
    }

    private static IdentityDbContext CreateDbContext()
    {
        return new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
    }

    private static ServiceProvider CreateIdentityServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<IdentityDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(bool forceReset, string password = TestPassword)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUTH_ADMIN_EMAIL"] = "ADMIN@gmail.com",
                ["AUTH_ADMIN_PASSWORD"] = password,
                ["AUTH_ADMIN_FORCE_PASSWORD_RESET"] = forceReset.ToString()
            })
            .Build();
    }

    private static IWebHostEnvironment CreateEnvironment(string environmentName = "Development")
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(env => env.EnvironmentName).Returns(environmentName);
        return environment.Object;
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
