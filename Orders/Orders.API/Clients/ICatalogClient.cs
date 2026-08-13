namespace Orders.API.Clients;

public interface ICatalogClient
{
    Task<CatalogProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}
