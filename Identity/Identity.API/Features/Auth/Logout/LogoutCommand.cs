namespace Identity.API.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) : ICommand<bool>;
