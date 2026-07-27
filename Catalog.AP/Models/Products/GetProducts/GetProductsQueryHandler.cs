using Catalog.API.Common.Caching;
using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts
{
    public record GetProductsQuery(
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<GetProductsResult>, ICacheableQuery
    {
        public string CacheKey => $"products-page-{PageNumber}-size-{PageSize}";
        public TimeSpan Expiration => TimeSpan.FromMinutes(5);
    }

    public record GetProductsResult(PaginateResult<Product> Products);

    internal class GetProductsQueryHandler
        (IDocumentSession session, ILogger<GetProductsQueryHandler> logger)
        : IQueryHandler<GetProductsQuery, GetProductsResult>
    {
        public async Task<GetProductsResult> Handle(GetProductsQuery query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "GetProductsQueryHandler.Handle llamado con {@Query}",
                query);


            var totalCount = await session.Query<Product>()
                .LongCountAsync(cancellationToken);

            var products = await session.Query<Product>()
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var paginateResult =
                  new PaginateResult<Product>
                  {
                      PageNumber = query.PageNumber,
                      PageSize = query.PageSize,
                      TotalCount = totalCount,
                      Data = products
                 };
            return new GetProductsResult(paginateResult);
        }
    }
}
