using BuildingBlocks.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Identity.API.Tests;

public class CorsPolicyTests
{
    private const string NetlifyOrigin = "https://eshop-services.netlify.app";

    [Fact]
    public void ProductionAcceptsConfiguredNetlifyOrigin()
    {
        var origins = BuildConfiguration(("Cors:AllowedOrigins:0", NetlifyOrigin))
            .GetRequiredCorsOrigins(new FakeEnvironment("Production"));

        Assert.Equal([NetlifyOrigin], origins);
    }

    [Fact]
    public void ProductionNormalizesTrailingSlash()
    {
        var origins = BuildConfiguration(("Cors:AllowedOrigins:0", $"{NetlifyOrigin}/"))
            .GetRequiredCorsOrigins(new FakeEnvironment("Production"));

        Assert.Equal([NetlifyOrigin], origins);
    }

    [Fact]
    public void ProductionRejectsMissingOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildConfiguration().GetRequiredCorsOrigins(new FakeEnvironment("Production")));

        Assert.Equal("CORS configuration is incomplete: Cors:AllowedOrigins is missing.", exception.Message);
    }

    [Fact]
    public void ProductionRejectsWildcardOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildConfiguration(("Cors:AllowedOrigins:0", "*")).GetRequiredCorsOrigins(new FakeEnvironment("Production")));

        Assert.Equal("CORS configuration is invalid: wildcard origins are not allowed.", exception.Message);
    }

    [Fact]
    public void DevelopmentKeepsLocalhostDefaults()
    {
        var origins = BuildConfiguration().GetRequiredCorsOrigins(new FakeEnvironment("Development"));

        Assert.Equal(["http://localhost:5173", "http://localhost:8088"], origins);
    }

    [Fact]
    public async Task LoginPreflightFromNetlify_ReturnsAllowOrigin()
    {
        var headers = await ExecutePreflightAsync(NetlifyOrigin, "/auth/login", "content-type");

        Assert.Equal(NetlifyOrigin, headers.AccessControlAllowOrigin);
    }

    [Fact]
    public async Task RefreshPreflightFromNetlify_ReturnsAllowCredentials()
    {
        var headers = await ExecutePreflightAsync(NetlifyOrigin, "/auth/refresh", "content-type");

        Assert.Equal(NetlifyOrigin, headers.AccessControlAllowOrigin);
        Assert.Equal("true", headers.AccessControlAllowCredentials);
    }

    [Fact]
    public async Task UnknownOrigin_DoesNotReturnAllowOrigin()
    {
        var headers = await ExecutePreflightAsync("https://unknown.example", "/auth/login", "content-type");

        Assert.False(headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    private static async Task<IHeaderDictionary> ExecutePreflightAsync(string origin, string path, string requestedHeaders)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
            {
                policy
                    .WithOrigins(NetlifyOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var context = CreatePreflightContext(origin, path, requestedHeaders);
        var policy = await serviceProvider.GetRequiredService<ICorsPolicyProvider>().GetPolicyAsync(context, "FrontendCors");
        var result = serviceProvider.GetRequiredService<ICorsService>().EvaluatePolicy(context, policy!);
        serviceProvider.GetRequiredService<ICorsService>().ApplyResult(result, context.Response);

        return context.Response.Headers;
    }

    private static DefaultHttpContext CreatePreflightContext(string origin, string path, string requestedHeaders)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Options;
        context.Request.Path = path;
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = HttpMethods.Post;
        context.Request.Headers.AccessControlRequestHeaders = requestedHeaders;
        return context;
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value))
            .Build();
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
