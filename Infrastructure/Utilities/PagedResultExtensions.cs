#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Extension methods for <see cref="PagedResult{T}"/> to provide common pagination operations.
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Gets the items for the current page as a read-only collection.
    /// Useful for thread-safe access and preventing accidental modifications.
    /// </summary>
    public static IReadOnlyList<T> AsReadOnly<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.AsReadOnly();
    }

    /// <summary>
    /// Gets a value indicating whether the result contains any items.
    /// </summary>
    public static bool HasItems<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0;
    }

    /// <summary>
    /// Gets a value indicating whether the result is empty (no items).
    /// </summary>
    public static bool IsEmpty<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count == 0;
    }

    /// <summary>
    /// Gets the current page items as a span for efficient processing without allocation.
    /// </summary>
    public static ReadOnlySpan<T> AsSpan<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result.Items);
    }

    /// <summary>
    /// Gets the first item in the page if it exists, or returns default(T) if the page is empty.
    /// </summary>
    public static T? FirstOrDefault<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0 ? result.Items[0] : default;
    }

    /// <summary>
    /// Gets the last item in the page if it exists, or returns default(T) if the page is empty.
    /// </summary>
    public static T? LastOrDefault<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0 ? result.Items[^1] : default;
    }

    /// <summary>
    /// Projects each item in the page using the specified selector function.
    /// </summary>
    public static PagedResult<TResult> Select<T, TResult>(
        this PagedResult<T> result,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);

        var selectedItems = result.Items.Select(selector).ToList();
        return new PagedResult<TResult>
        {
            Items = selectedItems,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Filters the page items using the specified predicate.
    /// </summary>
    public static PagedResult<T> Where<T>(
        this PagedResult<T> result,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);

        var filteredItems = result.Items.Where(predicate).ToList();
        return new PagedResult<T>
        {
            Items = filteredItems,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    /// <summary>
    /// Gets the items for the current page as an array.
    /// </summary>
    public static T[] ToArray<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.ToArray();
    }

    /// <summary>
    /// Gets the items for the current page as a list.
    /// </summary>
    public static List<T> ToList<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new List<T>(result.Items);
    }
}