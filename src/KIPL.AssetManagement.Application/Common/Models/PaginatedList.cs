using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Application.Common.Models;

public class PaginatedList<T>
{
    public PaginatedList(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = pageSize <= 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public int FirstRow => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int LastRow => Math.Min(PageNumber * PageSize, TotalCount);

    /// <summary>Pages a query that already projects to <typeparamref name="T"/>.</summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        Normalise(ref pageNumber, ref pageSize);

        var count = await source.CountAsync(ct);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }

    /// <summary>
    /// Pages an entity query in SQL, then maps the page in memory. Use this when the
    /// projection calls a helper method that EF Core cannot translate to SQL.
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync<TSource>(
        IQueryable<TSource> source, Func<TSource, T> map, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        Normalise(ref pageNumber, ref pageSize);

        var count = await source.CountAsync(ct);
        var rows = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PaginatedList<T>(rows.Select(map).ToList(), count, pageNumber, pageSize);
    }

    private static void Normalise(ref int pageNumber, ref int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
    }
}
