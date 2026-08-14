using Microsoft.Extensions.Configuration;

namespace Orders.API.Configuration;

public static class OrdersConfigurationExtensions
{
    public static MongoDbOptions GetRequiredMongoDbOptions(this IConfiguration configuration)
    {
        var options = configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>() ?? new MongoDbOptions();

        EnsureNotEmpty(options.ConnectionString, "MongoDb:ConnectionString");
        EnsureNotEmpty(options.DatabaseName, "MongoDb:DatabaseName");
        EnsureNotEmpty(options.OrdersCollection, "MongoDb:OrdersCollection");

        return options;
    }

    public static OrdersOptions GetRequiredOrdersOptions(this IConfiguration configuration)
    {
        var options = configuration.GetSection(OrdersOptions.SectionName).Get<OrdersOptions>() ?? new OrdersOptions();
        if (options.TaxRate < 0)
        {
            throw new InvalidOperationException("Orders configuration is invalid: Orders:TaxRate must be non-negative.");
        }

        return options;
    }

    public static HttpClientOptions GetRequiredHttpClientOptions(this IConfiguration configuration, string sectionName)
    {
        var options = configuration.GetSection(sectionName).Get<HttpClientOptions>() ?? new HttpClientOptions();
        EnsureNotEmpty(options.BaseAddress, $"{sectionName}:BaseAddress");

        var normalized = options.BaseAddress.Trim().TrimEnd('/') + "/";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"Orders configuration is invalid: {sectionName}:BaseAddress must be an absolute URI.");
        }

        options.BaseAddress = uri.ToString();
        return options;
    }

    private static void EnsureNotEmpty(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Orders configuration is incomplete: {key} is missing.");
        }
    }
}
