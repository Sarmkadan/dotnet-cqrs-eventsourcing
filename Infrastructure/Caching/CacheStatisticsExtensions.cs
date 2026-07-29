#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Caching;

/// <summary>
/// Extension methods for <see cref="CacheStatistics"/>.
/// </summary>
public static class CacheStatisticsExtensions
{
    /// <summary>
    /// Calculates the hit rate of the cache as hits / (hits + misses).
    /// If there are no entries, the hit rate is defined as 0 to avoid division by zero.
    /// </summary>
    /// <param name="stats">The cache statistics instance.</param>
    /// <returns>The hit rate as a value between 0 and 1.</returns>
    public static double HitRate(this CacheStatistics stats)
    {
        if (stats is null)
            throw new ArgumentNullException(nameof(stats));

        // TotalHits represents the number of cache hits.
        // Misses are inferred as the number of entries that were not hit.
        // In this simplified model, we treat the current number of entries as the
        // denominator for hit rate calculation. If there are no entries, return 0.
        if (stats.TotalEntries == 0)
            return 0.0;

        // Hit rate = hits / (hits + misses)
        // Since misses = TotalEntries - TotalHits in this context,
        // the formula simplifies to TotalHits / TotalEntries.
        return (double)stats.TotalHits / stats.TotalEntries;
    }

    /// <summary>
    /// Determines whether the cache is healthy based on a minimum hit rate threshold.
    /// </summary>
    /// <param name="stats">The cache statistics instance.</param>
    /// <param name="minHitRate">The minimum acceptable hit rate (0 to 1).</param>
    /// <returns><c>true</c> if the hit rate is greater than or equal to <paramref name="minHitRate"/>; otherwise, <c>false</c>.</returns>
    public static bool IsHealthy(this CacheStatistics stats, double minHitRate)
    {
        if (stats is null)
            throw new ArgumentNullException(nameof(stats));

        if (minHitRate < 0.0 || minHitRate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(minHitRate), "Minimum hit rate must be between 0 and 1.");

        return stats.HitRate() >= minHitRate;
    }

    /// <summary>
    /// Returns a human‑readable representation of the cache statistics.
    /// </summary>
    /// <param name="stats">The cache statistics instance.</param>
    /// <returns>A string containing key statistics.</returns>
    public static string ToDisplayString(this CacheStatistics stats)
    {
        if (stats is null)
            throw new ArgumentNullException(nameof(stats));

        return $"TotalEntries: {stats.TotalEntries}, TotalHits: {stats.TotalHits}, ExpiredEntries: {stats.ExpiredEntries}, AverageEntryAge: {stats.AverageEntryAge}";
    }
}
