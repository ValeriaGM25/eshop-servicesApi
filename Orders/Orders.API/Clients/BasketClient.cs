using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Orders.API.Clients;

public sealed class BasketClient(HttpClient httpClient, ILogger<BasketClient> logger) : IBasketClient
{
    public async Task<ShoppingCartDto> GetBasketAsync(string bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "basket");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new BadRequestException("Basket does not exist for the authenticated customer.");
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Basket API returned status {StatusCode} while reading basket.", response.StatusCode);
            throw new InternalServerException("Basket service is temporarily unavailable.");
        }

        var basketResponse = await response.Content.ReadFromJsonAsync<BasketResponse>(cancellationToken);
        return basketResponse?.Cart ?? throw new BadRequestException("Basket response is invalid.");
    }

    public async Task<bool> DeleteBasketAsync(string bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "basket");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Basket API returned status {StatusCode} while deleting basket after order persistence.", response.StatusCode);
            return false;
        }

        var deleteResponse = await response.Content.ReadFromJsonAsync<DeleteBasketResponse>(cancellationToken);
        return deleteResponse?.IsSuccess == true;
    }
}
