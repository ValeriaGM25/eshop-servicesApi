namespace Catalog.API.Common.Pagination
{
    public class PaginateResult<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public IEnumerable<T> Data { get; set; }
        = Enumerable.Empty<T>();


    }
}
