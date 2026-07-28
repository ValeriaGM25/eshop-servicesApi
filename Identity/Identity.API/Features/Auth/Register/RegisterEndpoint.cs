namespace Identity.API.Features.Auth.Register;

public record RegisterRequest(string FullName, string Email, string Password, string ConfirmPassword);

public record RegisterResponse(AuthUserDto User);

public class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            RegisterRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RegisterCommand(
                request.FullName,
                request.Email,
                request.Password,
                request.ConfirmPassword), cancellationToken);

            return Results.Created($"/auth/users/{result.User.Id}", new RegisterResponse(result.User));
        })
        .RequireRateLimiting("RegisterPolicy")
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
