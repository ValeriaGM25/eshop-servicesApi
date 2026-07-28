using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Catalog.API.Tests;

public class AuthorizationPolicyTests
{
    [Fact]
    public void AdminOnlyPolicy_AllowsOnlyAdminRole()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

        var requirement = Assert.Single(options.GetPolicy("AdminOnly")!.Requirements.OfType<RolesAuthorizationRequirement>());

        Assert.Equal(["Admin"], requirement.AllowedRoles);
    }
}
