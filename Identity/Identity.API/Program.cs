using Identity.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCarter();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = builder.Configuration["JWT_ISSUER"]
        ?? builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("JWT issuer is not configured.");
    options.Audience = builder.Configuration["JWT_AUDIENCE"]
        ?? builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("JWT audience is not configured.");
    options.Key = builder.Configuration["JWT_KEY"]
        ?? builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT key is not configured.");
    options.AccessTokenMinutes = ReadInt(builder.Configuration, "JWT_ACCESS_TOKEN_MINUTES", "Jwt:AccessTokenMinutes", 15);
    options.RefreshTokenDays = ReadInt(builder.Configuration, "JWT_REFRESH_TOKEN_DAYS", "Jwt:RefreshTokenDays", 7);
});

var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["JWT_KEY"] ?? builder.Configuration["Jwt:Key"];

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDatabase")));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        // Flexible only for local demo credentials in Development.
        options.Password.RequireUppercase = !builder.Environment.IsDevelopment();
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = !builder.Environment.IsDevelopment();
        options.Password.RequiredUniqueChars = builder.Environment.IsDevelopment() ? 1 : 4;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = CreateTokenValidationParameters(jwtIssuer, jwtAudience, jwtKey);
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:8088")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("RegisterPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 3;
        limiterOptions.Window = TimeSpan.FromMinutes(10);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("IdentityDatabase")!);

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services);

app.UseExceptionHandler();
app.UseCors("ReactApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();
app.MapOpenApi();
app.MapHealthChecks("/health");

app.Run();

static int ReadInt(IConfiguration configuration, string envKey, string configKey, int defaultValue)
{
    var value = configuration[envKey] ?? configuration[configKey];
    return int.TryParse(value, out var parsed) ? parsed : defaultValue;
}

static TokenValidationParameters CreateTokenValidationParameters(string? issuer, string? audience, string? key)
{
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
