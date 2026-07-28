namespace Identity.API.Features.Auth.Login;

public record LoginCommand(string Email, string Password, string? CreatedByIp) : ICommand<AuthResult>;
