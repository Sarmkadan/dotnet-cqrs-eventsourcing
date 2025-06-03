#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

/// <summary>
/// Extension methods for <see cref="CompactionResult"/> that provide additional
/// functionality for working with compaction results.
/// </summary>
public static class CompactionResultExtensions
{
    /// <summary>
    /// Creates a new <see cref="CompactionResult"/> with the events removed incremented by the specified delta.
    /// </summary>
    /// <param name="result">The original compaction result.</param>
    /// <param name="delta">The number of additional events to report as removed.</param>
    /// <returns>A new <see cref="CompactionResult"/> with updated event count.</returns>
    public static CompactionResult WithAdditionalEventsRemoved(this CompactionResult result, int delta)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CompactionResult(
            result.AggregateId,
            result.EventsRemoved + delta,
            result.CompactedToVersion,
            result.CompactedAt
        );
    }

    /// <summary>
    /// Creates a new <see cref="CompactionResult"/> with the compacted version adjusted by the specified delta.
    /// </summary>
    /// <param name="result">The original compaction result.</param>
    /// <param name="delta">The delta to apply to the compacted version.</param>
    /// <returns>A new <see cref="CompactionResult"/> with updated compacted version.</returns>
    public static CompactionResult WithVersionDelta(this CompactionResult result, long delta)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CompactionResult(
            result.AggregateId,
            result.EventsRemoved,
            result.CompactedToVersion + delta,
            result.CompactedAt
        );
    }

    /// <summary>
    /// Determines whether this compaction removed any events.
    /// </summary>
    /// <param name="result">The compaction result to check.</param>
    /// <returns>True if no events were removed; otherwise false.</returns>
    public static bool IsNoOp(this CompactionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.EventsRemoved == 0;
    }

    /// <summary>
    /// Formats the compaction result as a detailed message suitable for logging.
    /// </summary>
    /// <param name="result">The compaction result to format.</param>
    /// <returns>A formatted string with all compaction details.</returns>
    public static string ToDetailedString(this CompactionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return $"Compaction completed for aggregate {result.AggregateId} at {result.CompactedAt:O}: removed {result.EventsRemoved} events, compacted to version {result.CompactedToVersion}";
    }
}