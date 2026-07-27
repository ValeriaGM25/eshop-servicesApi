namespace Catalog.API.Models.Products.GetProductByCategory
{
    record GetProductByCategoryQuery(string Category)
        :IQuery<GetProductByCategoryResult>;

    record GetProductByCategoryResult(IEnumerable<Product> Products);

    internal class GetProductByCategoryHandler(
        IDocumentSession session, ILogger<GetProductByCategoryHandler>
        logger) : IQueryHandler<GetProductByCategoryQuery,
            GetProductByCategoryResult>
    {
        public async Task<GetProductByCategoryResult> Handle(GetProductByCategoryQuery query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "GetProductByCategoryQueryHandler.Handle llamado con {Query}",
                query);
            var products = await session.Query<Product>()
                .Where(p => p.Category.Contains(query.Category))
                .ToListAsync(cancellationToken);
            return new GetProductByCategoryResult(products);
        }
    }
    
}
