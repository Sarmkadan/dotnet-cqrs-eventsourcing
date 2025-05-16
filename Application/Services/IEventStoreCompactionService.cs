#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Shared.Results;

/// <summary>
/// Result returned by a compaction operation describing how many events were
/// removed and the version boundary that was used as the cut-off point.
/// </summary>
public sealed class CompactionResult
{
    /// <summary>Identifier of the aggregate whose events were compacted.</summary>
    public string AggregateId { get; init; }

    /// <summary>Number of events removed from the store.</summary>
    public int EventsRemoved { get; init; }

    /// <summary>
    /// The version up to which (exclusive) events were deleted.
    /// Events at this version and beyond are retained.
    /// </summary>
    public long CompactedToVersion { get; init; }

    /// <summary>UTC timestamp when the compaction was performed.</summary>
    public DateTime CompactedAt { get; init; }

    public CompactionResult(string aggregateId, int eventsRemoved, long compactedToVersion, DateTime compactedAt)
    {
        AggregateId = aggregateId;
        EventsRemoved = eventsRemoved;
        CompactedToVersion = compactedToVersion;
        CompactedAt = compactedAt;
    }

    public override string ToString()
        => $"CompactionResult {{ AggregateId={AggregateId}, Removed={EventsRemoved}, CompactedToVersion={CompactedToVersion} }}";
}

/// <summary>
/// Service for compacting the event store by pruning superseded events that have
/// already been captured in a snapshot.  Compaction reduces storage requirements
/// and speeds up aggregate reconstruction while preserving recoverability from
/// the latest snapshot forward.
/// </summary>
public interface IEventStoreCompactionService
{
    /// <summary>
    /// Compacts the event stream for the given aggregate.  All events whose version
    /// is strictly less than the latest available snapshot version are permanently
    /// removed from the repository.
    /// </summary>
    /// <param name="aggregateId">Identifier of the aggregate to compact.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="CompactionResult"/> describing the outcome, or a failure result
    /// when no snapshot exists or the repository reports an error.
    /// </returns>
    Task<Result<CompactionResult>> CompactAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts the event stream for the given aggregate by retaining all events at
    /// or after <paramref name="keepFromVersion"/> and deleting everything before it.
    /// Use this overload when you want explicit control over the cut-off point,
    /// independent of any snapshot.
    /// </summary>
    /// <param name="aggregateId">Identifier of the aggregate to compact.</param>
    /// <param name="keepFromVersion">
    /// Minimum version to retain (inclusive).  Events with a version strictly less
    /// than this value will be deleted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<CompactionResult>> CompactToVersionAsync(string aggregateId, long keepFromVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts all aggregates that have at least one snapshot.
    /// Aggregates without snapshots are silently skipped.
    /// </summary>
    /// <param name="aggregateIds">Aggregate identifiers to inspect and compact.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<IReadOnlyList<CompactionResult>>> CompactAllAsync(IEnumerable<string> aggregateIds, CancellationToken cancellationToken = default);
}
