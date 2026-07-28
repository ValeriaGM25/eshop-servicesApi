namespace Identity.API.Features.Auth.Refresh;

public record RefreshCommand(string RefreshToken, string? CreatedByIp) : ICommand<AuthResult>;
