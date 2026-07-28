namespace Identity.API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool createScope = true)
    {
        using var scope = createScope ? services.CreateScope() : null;
        var serviceProvider = scope?.ServiceProvider ?? services;
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        await EnsureRolesAsync(roleManager, logger);
        await SeedAdminAsync(userManager, configuration, environment, logger);
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
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var adminEmail = configuration["AUTH_ADMIN_EMAIL"]?.Trim().ToLowerInvariant();
        var adminPassword = configuration["AUTH_ADMIN_PASSWORD"];
        var adminFullName = configuration["AUTH_ADMIN_FULL_NAME"]?.Trim();
        var forcePasswordReset = bool.TryParse(configuration["AUTH_ADMIN_FORCE_PASSWORD_RESET"], out var parsed) && parsed;

        if (string.IsNullOrWhiteSpace(adminEmail)
            || string.IsNullOrWhiteSpace(adminPassword)
            || string.IsNullOrWhiteSpace(adminFullName))
        {
            logger.LogInformation("Initial admin was not configured.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminFullName,
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
        }
        else
        {
            admin.UserName = adminEmail;
            admin.Email = adminEmail;
            admin.FullName = adminFullName;
            admin.IsActive = true;

            var updateResult = await userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not update initial admin user: {FormatErrors(updateResult)}");
            }

            var currentRoles = await userManager.GetRolesAsync(admin);
            var rolesToRemove = currentRoles.Where(role => role != IdentityRoles.Admin).ToArray();
            if (rolesToRemove.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(admin, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException($"Could not remove unexpected admin roles: {FormatErrors(removeResult)}");
                }
            }

            if (!currentRoles.Contains(IdentityRoles.Admin))
            {
                var addRoleResult = await userManager.AddToRoleAsync(admin, IdentityRoles.Admin);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Could not assign Admin role to initial admin user: {FormatErrors(addRoleResult)}");
                }
            }

            logger.LogInformation("Initial admin user already exists.");
        }

        if (!forcePasswordReset)
        {
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var resetResult = await userManager.ResetPasswordAsync(admin, resetToken, adminPassword);
        if (!resetResult.Succeeded)
        {
            var message = $"Could not reset initial admin password: {FormatErrors(resetResult)}";
            logger.LogError("{Message}", message);
            if (environment.IsDevelopment())
            {
                throw new InvalidOperationException(message);
            }

            return;
        }

        logger.LogInformation("La contraseña del administrador local fue restablecida.");
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join(", ", result.Errors.Select(error => error.Code));
    }
}
