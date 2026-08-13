namespace Orders.API.Clients;

public sealed record CatalogProductResponse(CatalogProductDto Product);

public sealed class CatalogProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<string> Category { get; set; } = [];
    public string ImageFiles { get; set; } = default!;
    public decimal Price { get; set; }
}
