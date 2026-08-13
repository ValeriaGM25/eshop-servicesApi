namespace Orders.API.Configuration;

public sealed class OrdersOptions
{
    public const string SectionName = "Orders";
    public decimal TaxRate { get; set; } = 0.18m;
}
