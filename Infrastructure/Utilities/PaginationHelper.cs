#nullable enable
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
    /// <summary>Gets or sets the items on the current page.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Gets or sets the one-based number of the current page.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the maximum number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the total number of items across all pages.</summary>
    public long TotalCount { get; set; }

    /// <summary>Gets the total number of pages, derived from <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Gets a value indicating whether a page precedes the current page.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Gets a value indicating whether a page follows the current page.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Returns a string representation of the paging state and items.
    /// </summary>
    /// <returns>A string describing the items, page number, page size and total count.</returns>
    public override string ToString() => $"PagedResult {{ Items = {Items}, PageNumber = {PageNumber}, PageSize = {PageSize}, TotalCount = {TotalCount} }}";
}

/// <summary>
/// Static helpers for paginating collections and queryables and validating paging parameters.
/// </summary>
public static class PaginationHelper
{
    internal const int DefaultPageNumber = 1;
    internal const int DefaultPageSize = 20;
    internal const int MinPageNumber = 1;
    internal const int MinPageSize = 1;
    internal const int MaxPageSize = 1000;

    /// <summary>
    /// Paginates a collection with given page number and size.
    /// </summary>
    public static PagedResult<T> Paginate<T>(
        IEnumerable<T> items,
        int pageNumber = DefaultPageNumber,
        int pageSize = DefaultPageSize)
    {
        GuardClauses.InRange(pageNumber, MinPageNumber, int.MaxValue, nameof(pageNumber));
        GuardClauses.InRange(pageSize, MinPageSize, MaxPageSize, nameof(pageSize));

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
        int pageNumber = DefaultPageNumber,
        int pageSize = DefaultPageSize)
    {
        GuardClauses.InRange(pageNumber, MinPageNumber, int.MaxValue, nameof(pageNumber));
        GuardClauses.InRange(pageSize, MinPageSize, MaxPageSize, nameof(pageSize));

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
        var validPageNumber = Math.Max(MinPageNumber, pageNumber ?? DefaultPageNumber);
        var validPageSize = Math.Clamp(pageSize ?? DefaultPageSize, MinPageSize, MaxPageSize);

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
/// Extension methods for <see cref="IQueryable{T}"/> to simplify pagination.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Paginates an IQueryable directly.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IQueryable<T> query,
        int pageNumber = PaginationHelper.DefaultPageNumber,
        int pageSize = PaginationHelper.DefaultPageSize)
    {
        return PaginationHelper.PaginateQuery(query, pageNumber, pageSize);
    }

    /// <summary>
    /// Paginates an IEnumerable directly.
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(
        this IEnumerable<T> items,
        int pageNumber = PaginationHelper.DefaultPageNumber,
        int pageSize = PaginationHelper.DefaultPageSize)
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
