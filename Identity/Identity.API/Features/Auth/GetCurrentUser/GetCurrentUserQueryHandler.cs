namespace Identity.API.Features.Auth.GetCurrentUser;

public class GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetCurrentUserQuery, GetCurrentUserResult>
{
    public async Task<GetCurrentUserResult> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return new GetCurrentUserResult(new AuthUserDto(user.Id, user.FullName, user.Email!, roles.ToArray()));
    }
}
