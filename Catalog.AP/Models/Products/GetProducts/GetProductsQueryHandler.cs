using Catalog.API.Common.Pagination;

namespace Catalog.API.Models.Products.GetProducts
{
    public record GetProductsQuery(
        int PageNumber = 1,
        int PageSize = 10)
        : IQuery<GetProductsResult>;

    public record GetProductsResult(PaginateResult<Product> Products);

    public class GetProductsQueryHandler
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
                .OrderBy(product => product.Name)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return CreateResult(query, products, totalCount);
        }

        public static GetProductsResult CreateResult(
            GetProductsQuery query,
            IEnumerable<Product> products,
            long totalCount) =>
            new(new PaginateResult<Product>
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                Data = products
            });
    }
}
