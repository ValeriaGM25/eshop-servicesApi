using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var databaseConnectionString = builder.Configuration.GetRequiredConnectionString("Database");
var redisConnectionString = builder.Configuration.GetRequiredConnectionString("Redis");
var resilientRedisConnectionString = RedisConnectionConfiguration.BuildRedisConnectionString(redisConnectionString);
var jwtConfiguration = builder.Configuration.GetRequiredJwtConfiguration();
var corsOrigins = builder.Configuration.GetRequiredCorsOrigins(builder.Environment);

builder.Services.AddSingleton<ReadinessState>();
builder.Services.AddHostedService<BasketDependencyReadinessHostedService>();

builder.Services.AddCarter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
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
    options.Connection(databaseConnectionString);

    options.Schema
        .For<ShoppingCart>()
        .Identity(cart => cart.UserName);
})
.UseLightweightSessions();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = resilientRedisConnectionString;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(RedisConnectionConfiguration.BuildRedisConfigurationOptions(redisConnectionString)));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwtConfiguration.CreateTokenValidationParameters();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClienteOnly", policy => policy.RequireRole("Cliente"));
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck<ReadinessHealthCheck>("basket-readiness", tags: ["ready"])
    .AddNpgSql(databaseConnectionString, name: "basket-postgresql", tags: ["ready"])
    .AddRedis(resilientRedisConnectionString, name: "basket-redis", tags: ["ready"]);

var app = builder.Build();

app.Logger.LogInformation("Starting Basket API service.");

app.MapOpenApi();

app.UseExceptionHandler();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("FrontendCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
