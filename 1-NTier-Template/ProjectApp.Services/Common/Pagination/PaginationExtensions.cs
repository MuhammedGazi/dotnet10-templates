using Microsoft.EntityFrameworkCore;

namespace ProjectApp.Services.Common.Pagination;

public static class PaginationExtensions
{
    /// <summary>
    /// Sorguyu sayfalar. Tutarlı sayfalama için çağıran taraf sorguya önce bir <c>OrderBy</c> uygulamalıdır.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        if (count == 0)
        {
            return new PagedResult<T>([], 0, pageNumber, pageSize);
        }

        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }

    public static Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        PagedRequest request,
        CancellationToken cancellationToken = default)
        => source.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
}
