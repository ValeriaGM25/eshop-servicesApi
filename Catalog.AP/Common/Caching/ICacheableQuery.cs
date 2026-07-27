namespace Catalog.API.Common.Caching
{
    public interface ICacheableQuery
    {
        string CacheKey { get; }
        TimeSpan Expiration { get; }
    }
}
