using Identity.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.API.Tests;

internal static class IdentityTestHelpers
{
    public static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserPasswordStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
    }

    public static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
    {
        return new Mock<SignInManager<ApplicationUser>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }

    public static Mock<RoleManager<ApplicationRole>> MockRoleManager(IQueryable<ApplicationRole>? roles = null)
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        var manager = new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<ApplicationRole>>>());

        manager.Setup(manager => manager.Roles)
            .Returns(roles ?? Array.Empty<ApplicationRole>().AsQueryable());

        return manager;
    }
}
