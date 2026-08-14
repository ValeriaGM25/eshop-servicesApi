namespace Orders.API.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
    public string OrdersCollection { get; set; } = default!;
}
