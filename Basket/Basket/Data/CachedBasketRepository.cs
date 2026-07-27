using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace Basket.Data;

public class CachedBasketRepository(
    IBasketRepository repository,
    IDistributedCache cache,
    IConnectionMultiplexer connectionMultiplexer) : IBasketRepository
{
    private const string CacheKeyPrefix = "basket:";

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    private readonly IDatabase redisDatabase = connectionMultiplexer.GetDatabase();

    public async Task<ShoppingCart> GetBasket(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var cachedBasket = await cache.GetStringAsync(GetCacheKey(userName), cancellationToken);

        if (!string.IsNullOrWhiteSpace(cachedBasket))
        {
            var basket = JsonSerializer.Deserialize<ShoppingCart>(cachedBasket);
            if (basket is not null)
            {
                return basket;
            }
        }

        var storedBasket = await repository.GetBasket(userName, cancellationToken);
        await SetBasketCache(storedBasket, cancellationToken);

        return storedBasket;
    }

    public async Task<ShoppingCart> StoreBasket(
        ShoppingCart basket,
        CancellationToken cancellationToken = default)
    {
        var storedBasket = await repository.StoreBasket(basket, cancellationToken);
        await SetBasketCache(storedBasket, cancellationToken);

        return storedBasket;
    }

    public async Task<bool> DeleteBasket(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var isDeleted = await repository.DeleteBasket(userName, cancellationToken);
        await cache.RemoveAsync(GetCacheKey(userName), cancellationToken);
        await redisDatabase.KeyDeleteAsync(userName);

        return isDeleted;
    }

    private async Task SetBasketCache(
        ShoppingCart basket,
        CancellationToken cancellationToken)
    {
        var serializedBasket = JsonSerializer.Serialize(basket);

        await cache.SetStringAsync(
            GetCacheKey(basket.UserName),
            serializedBasket,
            CacheOptions,
            cancellationToken);

        await redisDatabase.StringSetAsync(
            basket.UserName,
            serializedBasket,
            TimeSpan.FromHours(24));
    }

    private static string GetCacheKey(string userName) => $"{CacheKeyPrefix}{userName}";
}
