namespace ProjectApp.Services.Common.Pagination;

/// <summary>
/// Sayfalı sorgu sonucu. <see cref="PagedRequest"/> ile birlikte kullanılır.
/// </summary>
public sealed class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
