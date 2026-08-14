using Microsoft.Extensions.Configuration;
using Orders.API.Configuration;

namespace Orders.API.Tests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void Missing_mongodb_connection_string_throws_clear_error()
    {
        var configuration = BuildConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredMongoDbOptions());

        Assert.Equal("Orders configuration is incomplete: MongoDb:ConnectionString is missing.", exception.Message);
    }

    [Fact]
    public void Missing_mongodb_database_name_throws_clear_error()
    {
        var configuration = BuildConfiguration(("MongoDb:ConnectionString", "mongodb://example"));

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredMongoDbOptions());

        Assert.Equal("Orders configuration is incomplete: MongoDb:DatabaseName is missing.", exception.Message);
    }

    [Fact]
    public void Missing_mongodb_collection_throws_clear_error()
    {
        var configuration = BuildConfiguration(
            ("MongoDb:ConnectionString", "mongodb://example"),
            ("MongoDb:DatabaseName", "EshopOrders"));

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredMongoDbOptions());

        Assert.Equal("Orders configuration is incomplete: MongoDb:OrdersCollection is missing.", exception.Message);
    }

    [Theory]
    [InlineData("BasketApi")]
    [InlineData("CatalogApi")]
    public void Missing_http_client_base_address_throws_clear_error(string sectionName)
    {
        var configuration = BuildConfiguration();

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredHttpClientOptions(sectionName));

        Assert.Equal($"Orders configuration is incomplete: {sectionName}:BaseAddress is missing.", exception.Message);
    }

    [Theory]
    [InlineData("BasketApi")]
    [InlineData("CatalogApi")]
    public void Invalid_http_client_base_address_throws_clear_error(string sectionName)
    {
        var configuration = BuildConfiguration(($"{sectionName}:BaseAddress", "not-a-url"));

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredHttpClientOptions(sectionName));

        Assert.Equal($"Orders configuration is invalid: {sectionName}:BaseAddress must be an absolute URI.", exception.Message);
    }

    [Fact]
    public void Negative_tax_rate_throws_clear_error()
    {
        var configuration = BuildConfiguration(("Orders:TaxRate", "-0.01"));

        var exception = Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredOrdersOptions());

        Assert.Equal("Orders configuration is invalid: Orders:TaxRate must be non-negative.", exception.Message);
    }

    [Fact]
    public void Azure_orders_configuration_is_resolved()
    {
        var configuration = BuildConfiguration(
            ("MongoDb:ConnectionString", "mongodb://example"),
            ("MongoDb:DatabaseName", "EshopOrders"),
            ("MongoDb:OrdersCollection", "orders"),
            ("Orders:TaxRate", "0.18"),
            ("BasketApi:BaseAddress", "http://eshop-basket-api"),
            ("CatalogApi:BaseAddress", "http://eshop-catalog-api"));

        var mongo = configuration.GetRequiredMongoDbOptions();
        var orders = configuration.GetRequiredOrdersOptions();
        var basket = configuration.GetRequiredHttpClientOptions("BasketApi");
        var catalog = configuration.GetRequiredHttpClientOptions("CatalogApi");

        Assert.Equal("EshopOrders", mongo.DatabaseName);
        Assert.Equal("orders", mongo.OrdersCollection);
        Assert.Equal(0.18m, orders.TaxRate);
        Assert.Equal("http://eshop-basket-api/", basket.BaseAddress);
        Assert.Equal("http://eshop-catalog-api/", catalog.BaseAddress);
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
    }
}
