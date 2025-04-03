namespace MiniApiGen.Filter;

public class PagedResult<T>
{
    public object Data { get; set; } = null!;
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize != 0 
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;
}