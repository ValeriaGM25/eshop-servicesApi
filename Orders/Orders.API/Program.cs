using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var jwtConfiguration = builder.Configuration.GetRequiredJwtConfiguration();
var corsOrigins = builder.Configuration.GetRequiredCorsOrigins(builder.Environment);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<OrdersOptions>(builder.Configuration.GetSection(OrdersOptions.SectionName));
builder.Services.Configure<HttpClientOptions>("BasketApi", builder.Configuration.GetSection("BasketApi"));
builder.Services.Configure<HttpClientOptions>("CatalogApi", builder.Configuration.GetSection("CatalogApi"));

builder.Services.AddSingleton<ReadinessState>();
builder.Services.AddHostedService<OrdersMongoInitializationHostedService>();
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.ConnectionString))
    {
        throw new InvalidOperationException("MongoDB configuration is incomplete: MongoDb:ConnectionString is missing.");
    }

    return new MongoClient(options.ConnectionString);
});
builder.Services.AddScoped<IOrdersRepository, MongoOrdersRepository>();
builder.Services.AddScoped<IOrderReportService, QuestPdfOrderReportService>();
builder.Services.AddHttpClient<IBasketClient, BasketClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get("BasketApi");
    client.BaseAddress = new Uri(options.BaseAddress.TrimEnd('/') + "/");
});
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptionsMonitor<HttpClientOptions>>().Get("CatalogApi");
    client.BaseAddress = new Uri(options.BaseAddress.TrimEnd('/') + "/");
});

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
    options.AddPolicy("FrontendCors", policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwtConfiguration.CreateTokenValidationParameters();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClienteOnly", policy => policy.RequireRole("Cliente"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ClienteOrAdmin", policy => policy.RequireRole("Cliente", "Admin"));
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<ReadinessHealthCheck>("orders-readiness", tags: ["ready"])
    .AddCheck<OrdersMongoHealthCheck>("orders-mongodb", tags: ["ready"]);

var app = builder.Build();

app.Logger.LogInformation("Starting Orders API service.");
app.UseExceptionHandler();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();
app.MapOpenApi();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.Run();

public partial class Program;
