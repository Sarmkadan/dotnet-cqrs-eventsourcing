// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Pagination helper for splitting large result sets into pages.
/// Prevents loading entire datasets into memory; improves performance and scalability.
/// Supports standard LIMIT/OFFSET pagination patterns.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public static class PaginationHelper
{
    /// <summary>
    /// Paginates a collection with given page number and size.
    /// </summary>
    public static PagedResult<T> Paginate<T>(
        IEnumerable<T> items,
        int pageNumber = 1,
        int pageSize = 20)
    {
        GuardClauses.InRange(pageNumber, 1, int.MaxValue, nameof(pageNumber));
        GuardClauses.InRange(pageSize, 1, 1000, nameof(pageSize));

        var itemsList = items.ToList();
        var totalCount = itemsList.Count;

        var pagedItems = itemsList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Paginates a queryable with given page number and size.
    /// Executes the query only for the requested page (database-level pagination).
    /// </summary>
    public static PagedResult<T> PaginateQuery<T>(
        IQueryable<T> query,
        int pageNumber = 1,
        int pageSize = 20)
    {
        GuardClauses.InRange(pageNumber, 1, int.MaxValue, nameof(pageNumber));
        GuardClauses.InRange(pageSize, 1, 1000, nameof(pageSize));

        var totalCount = query.Count();

        var pagedItems = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Validates pagination parameters and returns defaults if invalid.
    /// </summary>
    public static (int pageNumber, int pageSize) ValidatePaginationParams(int? pageNumber, int? pageSize)
    {
        var validPageNumber = Math.Max(1, pageNumber ?? 1);
        var validPageSize = Math.Clamp(pageSize ?? 20, 1, 1000);

        return (validPageNumber, validPageSize);
    }

    /// <summary>
    /// Gets start and end indices for a page.
    /// Useful for offset-based APIs.
    /// </summary>
    public static (int offset, int limit) GetOffsetAndLimit(int pageNumber, int pageSize)
    {
        return ((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Extension methods for IQueryable<T> to simplify pagination.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Paginates an IQueryable directly.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IQueryable<T> query,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return PaginationHelper.PaginateQuery(query, pageNumber, pageSize);
    }

    /// <summary>
    /// Paginates an IEnumerable directly.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IEnumerable<T> items,
        int pageNumber = 1,
        int pageSize = 20)
    {
        return PaginationHelper.Paginate(items, pageNumber, pageSize);
    }

    /// <summary>
    /// Converts a PagedResult to JSON-friendly format for API responses.
    /// </summary>
    public static object ToApiResponse<T>(this PagedResult<T> result)
    {
        return new
        {
            data = result.Items,
            pagination = new
            {
                pageNumber = result.PageNumber,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
                hasNextPage = result.HasNextPage,
                hasPreviousPage = result.HasPreviousPage
            }
        };
    }
}
