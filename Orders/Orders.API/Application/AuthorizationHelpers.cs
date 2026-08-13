namespace Orders.API.Application;

public static class AuthorizationHelpers
{
    public static string GetAuthenticatedCustomerId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Authenticated customer could not be resolved.");
    }

    public static string GetAuthenticatedCustomerName(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? throw new UnauthorizedAccessException("Authenticated customer name could not be resolved.");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }

    public static void EnsureCanAccessCustomer(this ClaimsPrincipal principal, string customerId)
    {
        if (!principal.IsAdmin() && !string.Equals(principal.GetAuthenticatedCustomerId(), customerId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Customer is not authorized to access this order resource.");
        }
    }
}
