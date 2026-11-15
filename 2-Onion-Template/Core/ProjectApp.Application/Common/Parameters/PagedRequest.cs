namespace ProjectApp.Application.Common.Parameters;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    // (PageNumber - 1) * PageSize int sınırını aşıp negatif OFFSET üretmesin diye üst sınır (boş sayfa döner).
    private const int MaxPageNumber = 1_000_000;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value <= 0 ? 1 : Math.Min(value, MaxPageNumber);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value <= 0 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }
}
