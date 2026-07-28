namespace Identity.API.Features.Auth.Register;

public class RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new ConflictException("El correo ya está registrado.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = command.FullName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, IdentityRoles.Cliente);
        if (!roleResult.Succeeded)
        {
            throw new InternalServerException("No se pudo asignar el rol Cliente.");
        }

        return new RegisterResult(new AuthUserDto(user.Id, user.FullName, user.Email!, [IdentityRoles.Cliente]));
    }
}
