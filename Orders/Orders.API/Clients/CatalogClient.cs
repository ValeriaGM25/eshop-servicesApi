using System.Net;
using System.Net.Http.Json;

namespace Orders.API.Clients;

public sealed class CatalogClient(HttpClient httpClient, ILogger<CatalogClient> logger) : ICatalogClient
{
    public async Task<CatalogProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"products/{productId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Catalog API returned status {StatusCode} for product {ProductId}.", response.StatusCode, productId);
            throw new InternalServerException("Catalog service is temporarily unavailable.");
        }

        var productResponse = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(cancellationToken);
        return productResponse?.Product;
    }
}
