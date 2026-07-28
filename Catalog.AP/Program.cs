using Catalog.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

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

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
}).UseLightweightSessions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = CreateTokenValidationParameters(builder.Configuration);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await CatalogInitialData.SeedAsync(app.Services);
}

app.UseExceptionHandler();

app.UseCors("ReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapOpenApi();

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
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
}
