namespace Identity.API.Features.Auth.Register;

public record RegisterCommand(string FullName, string Email, string Password, string ConfirmPassword)
    : ICommand<RegisterResult>;

public record RegisterResult(AuthUserDto User);
