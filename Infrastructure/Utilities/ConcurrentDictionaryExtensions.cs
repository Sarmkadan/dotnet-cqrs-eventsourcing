// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Provides thread-safe extension methods for <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// </summary>
public static class ConcurrentDictionaryExtensions
{
    /// <summary>
    /// Atomically removes the entry only if both the key and the specific value instance exist.
    /// This prevents race conditions where a newer entry with the same key might be accidentally removed.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="dictionary">The concurrent dictionary.</param>
    /// <param name="key">The key of the entry to remove.</param>
    /// <param name="value">The specific value instance that must be present to remove the entry.</param>
    /// <returns>True if the entry was removed; otherwise, false.</returns>
    public static bool TryRemoveEntry<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        where TValue : class
    {
        return dictionary.TryRemove(new KeyValuePair<TKey, TValue>(key, value));
    }
}
