using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using MongoDB.Driver;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var jwtConfiguration = builder.Configuration.GetRequiredJwtConfiguration();
var corsOrigins = builder.Configuration.GetRequiredCorsOrigins(builder.Environment);
var mongoDbOptions = builder.Configuration.GetRequiredMongoDbOptions();
builder.Configuration.GetRequiredOrdersOptions();
var basketApiOptions = builder.Configuration.GetRequiredHttpClientOptions("BasketApi");
var catalogApiOptions = builder.Configuration.GetRequiredHttpClientOptions("CatalogApi");

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<OrdersOptions>(builder.Configuration.GetSection(OrdersOptions.SectionName));
builder.Services.Configure<HttpClientOptions>("BasketApi", builder.Configuration.GetSection("BasketApi"));
builder.Services.Configure<HttpClientOptions>("CatalogApi", builder.Configuration.GetSection("CatalogApi"));

builder.Services.AddSingleton<ReadinessState>();
builder.Services.AddHostedService<OrdersMongoInitializationHostedService>();
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(mongoDbOptions.ConnectionString);
});
builder.Services.AddScoped<IOrdersRepository, MongoOrdersRepository>();
builder.Services.AddScoped<IOrderReportService, QuestPdfOrderReportService>();
builder.Services.AddHttpClient<IBasketClient, BasketClient>((sp, client) =>
{
    client.BaseAddress = new Uri(basketApiOptions.BaseAddress);
});
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>((sp, client) =>
{
    client.BaseAddress = new Uri(catalogApiOptions.BaseAddress);
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
