namespace Identity.API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool createScope = true)
    {
        using var scope = createScope ? services.CreateScope() : null;
        var serviceProvider = scope?.ServiceProvider ?? services;
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var dbContext = serviceProvider.GetRequiredService<IdentityDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        await EnsureRolesAsync(roleManager, logger);
        await SeedAdminAsync(userManager, dbContext, configuration, environment, logger);
    }

    public static async Task EnsureRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        var existingRoles = roleManager.Roles.ToList();
        foreach (var role in existingRoles.Where(role => role.Name is not null && !IdentityRoles.All.Contains(role.Name)))
        {
            var deleteResult = await roleManager.DeleteAsync(role);
            if (!deleteResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not delete unexpected role {role.Name}: {FormatErrors(deleteResult)}");
            }

            logger.LogInformation("Unexpected identity role removed: {Role}", role.Name);
        }

        foreach (var roleName in IdentityRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create role {roleName}: {FormatErrors(result)}");
                }

                logger.LogInformation("Identity role created: {Role}", roleName);
            }
        }
    }

    public static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        IdentityDbContext dbContext,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var adminOptions = AdminBootstrapOptions.FromConfiguration(configuration);
        adminOptions.ValidateForProduction(environment);

        if (string.IsNullOrWhiteSpace(adminOptions.Email))
        {
            logger.LogInformation("Initial admin was not configured.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (admin is null)
        {
            var adminPassword = adminOptions.GetRequiredPasswordForCreate();
            admin = new ApplicationUser
            {
                UserName = adminOptions.Email,
                Email = adminOptions.Email,
                FullName = adminOptions.FullName,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create initial admin user: {FormatErrors(createResult)}");
            }

            var roleResult = await userManager.AddToRoleAsync(admin, IdentityRoles.Admin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign Admin role to initial admin user: {FormatErrors(roleResult)}");
            }

            logger.LogInformation("Initial admin user created.");
            logger.LogInformation("Admin role assigned to initial admin user.");
            return;
        }

        if (!adminOptions.ForcePasswordReset)
        {
            logger.LogInformation("Initial admin user already exists. Password reset is disabled.");
            return;
        }

        var resetPassword = adminOptions.GetRequiredPasswordForReset();
        await EnsureAdminRoleAsync(userManager, admin);
        await ResetAdminPasswordAsync(userManager, admin, resetPassword, logger);
        await RevokeActiveRefreshTokensAsync(dbContext, admin.Id, logger);
    }

    private static async Task EnsureAdminRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser admin)
    {
        if (await userManager.IsInRoleAsync(admin, IdentityRoles.Admin))
        {
            return;
        }

        var addRoleResult = await userManager.AddToRoleAsync(admin, IdentityRoles.Admin);
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not assign Admin role to initial admin user: {FormatErrors(addRoleResult)}");
        }
    }

    private static async Task ResetAdminPasswordAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser admin,
        string adminPassword,
        ILogger logger)
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var resetResult = await userManager.ResetPasswordAsync(admin, resetToken, adminPassword);
        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not reset initial admin password: {FormatErrors(resetResult)}");
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(admin);
        if (!stampResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not update initial admin security stamp: {FormatErrors(stampResult)}");
        }

        logger.LogInformation("Initial admin password reset completed.");
    }

    private static async Task RevokeActiveRefreshTokensAsync(
        IdentityDbContext dbContext,
        string adminUserId,
        ILogger logger)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == adminUserId
                && token.RevokedAtUtc == null
                && token.ExpiresAtUtc > now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
        }

        if (activeTokens.Count > 0)
        {
            await dbContext.SaveChangesAsync();
        }

        logger.LogInformation("Existing admin refresh tokens revoked.");
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join(", ", result.Errors.Select(error => error.Code));
    }
}
