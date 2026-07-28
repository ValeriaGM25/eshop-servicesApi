namespace Identity.API.Features.Auth.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password)
            .WithMessage("Las contraseñas no coinciden.");
    }
}
