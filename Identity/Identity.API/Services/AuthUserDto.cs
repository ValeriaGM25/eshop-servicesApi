namespace Identity.API.Services;

public record AuthUserDto(string Id, string FullName, string Email, IReadOnlyCollection<string> Roles);

public record AuthResult(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User, string RefreshToken);

public record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User);
