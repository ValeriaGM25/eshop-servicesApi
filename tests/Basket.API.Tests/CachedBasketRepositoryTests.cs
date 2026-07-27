using Basket.Data;
using Basket.Models;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Basket.API.Tests;

public class CachedBasketRepositoryTests
{
    [Fact]
    public async Task GetBasket_ReturnsCachedBasket_WhenCacheEntryExists()
    {
        var cart = new ShoppingCart { UserName = "valeria", Items = [] };
        var serializedCart = JsonSerializer.Serialize(cart);
        var cache = new FakeDistributedCache(serializedCart);
        var repository = new Mock<IBasketRepository>();
        var connection = new Mock<IConnectionMultiplexer>();
        connection
            .Setup(multiplexer => multiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(Mock.Of<IDatabase>());

        var cachedRepository = new CachedBasketRepository(repository.Object, cache, connection.Object);

        var result = await cachedRepository.GetBasket("valeria", CancellationToken.None);

        Assert.Equal("valeria", result.UserName);
        repository.Verify(repo => repo.GetBasket(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FakeDistributedCache(string cachedValue) : IDistributedCache
    {
        public byte[]? Get(string key) => Encoding.UTF8.GetBytes(cachedValue);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(cachedValue));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
            Task.CompletedTask;
    }
}
