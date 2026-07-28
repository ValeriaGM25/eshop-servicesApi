namespace Identity.API.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery(string UserId) : IQuery<GetCurrentUserResult>;

public record GetCurrentUserResult(AuthUserDto User);
