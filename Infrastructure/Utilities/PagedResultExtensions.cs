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
    /// <returns>A read-only collection containing the items for the current page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<T> AsReadOnly<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.AsReadOnly();
    }

    /// <summary>
    /// Gets a value indicating whether the result contains any items.
    /// </summary>
    /// <returns><see langword="true"/> if the result contains any items; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static bool HasItems<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0;
    }

    /// <summary>
    /// Gets a value indicating whether the result is empty (no items).
    /// </summary>
    /// <returns><see langword="true"/> if the result is empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static bool IsEmpty<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count == 0;
    }

    /// <summary>
    /// Gets the current page items as a span for efficient processing without allocation.
    /// </summary>
    /// <returns>A span representing the items in the current page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static ReadOnlySpan<T> AsSpan<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(result.Items);
    }

    /// <summary>
    /// Gets the first item in the page if it exists, or returns default(T) if the page is empty.
    /// </summary>
    /// <returns>The first item in the page, or <see langword="default(T)"/> if the page is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static T? FirstOrDefault<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0 ? result.Items[0] : default;
    }

    /// <summary>
    /// Gets the last item in the page if it exists, or returns default(T) if the page is empty.
    /// </summary>
    /// <returns>The last item in the page, or <see langword="default(T)"/> if the page is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static T? LastOrDefault<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.Count > 0 ? result.Items[^1] : default;
    }

    /// <summary>
    /// Projects each item in the page using the specified selector function.
    /// </summary>
    /// <param name="selector">A transform function to apply to each item.</param>
    /// <returns>A new <see cref="PagedResult{TResult}"/> with the projected items.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is <see langword="null"/>.
    /// Thrown when <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
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
    /// <param name="predicate">A function to test each item for a condition.</param>
    /// <returns>A new <see cref="PagedResult{T}"/> containing only the items that match the predicate.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is <see langword="null"/>.
    /// Thrown when <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
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
    /// <returns>An array containing the items for the current page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static T[] ToArray<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Items.ToArray();
    }

    /// <summary>
    /// Gets the items for the current page as a list.
    /// </summary>
    /// <returns>A list containing the items for the current page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <see langword="null"/>.</exception>
    public static List<T> ToList<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new List<T>(result.Items);
    }
}