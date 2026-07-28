using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:8088")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Database")!);

    options.Schema
        .For<ShoppingCart>()
        .Identity(cart => cart.UserName);
})
.UseLightweightSessions();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = CreateTokenValidationParameters(builder.Configuration);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClienteOnly", policy => policy.RequireRole("Cliente"));
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

app.MapOpenApi();

app.UseExceptionHandler();

app.UseCors("ReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapHealthChecks("/health");

app.Run();

static TokenValidationParameters CreateTokenValidationParameters(IConfiguration configuration)
{
    var issuer = configuration["Jwt:Issuer"];
    var audience = configuration["Jwt:Audience"];
    var key = configuration["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(issuer)
        || string.IsNullOrWhiteSpace(audience)
        || string.IsNullOrWhiteSpace(key))
    {
        throw new InvalidOperationException("JWT configuration is incomplete.");
    }

    return new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
}
