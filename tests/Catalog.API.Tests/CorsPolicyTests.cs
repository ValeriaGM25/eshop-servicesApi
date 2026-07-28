using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.API.Tests;

public class CorsPolicyTests
{
    private const string NetlifyOrigin = "https://eshop-services.netlify.app";

    [Fact]
    public async Task PreflightFromNetlify_IsAccepted()
    {
        var headers = await ExecutePreflightAsync(NetlifyOrigin, "authorization,content-type");

        Assert.Equal(NetlifyOrigin, headers.AccessControlAllowOrigin);
    }

    [Fact]
    public async Task AuthorizationHeader_IsAllowed()
    {
        var headers = await ExecutePreflightAsync(NetlifyOrigin, "authorization");

        Assert.Contains("authorization", headers.AccessControlAllowHeaders.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownOrigin_IsRejected()
    {
        var headers = await ExecutePreflightAsync("https://unknown.example", "authorization");

        Assert.False(headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    private static async Task<IHeaderDictionary> ExecutePreflightAsync(string origin, string requestedHeaders)
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
                    .AllowAnyMethod();
            });
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Options;
        context.Request.Path = "/products";
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = HttpMethods.Get;
        context.Request.Headers.AccessControlRequestHeaders = requestedHeaders;

        var policy = await serviceProvider.GetRequiredService<ICorsPolicyProvider>().GetPolicyAsync(context, "FrontendCors");
        var result = serviceProvider.GetRequiredService<ICorsService>().EvaluatePolicy(context, policy!);
        serviceProvider.GetRequiredService<ICorsService>().ApplyResult(result, context.Response);

        return context.Response.Headers;
    }
}
