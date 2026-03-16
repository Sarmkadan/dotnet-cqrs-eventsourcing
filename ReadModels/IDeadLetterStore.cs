#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Represents a domain event that could not be processed by a projection runner
/// after all retry attempts were exhausted.
/// </summary>
public sealed class DeadLetterEntry
{
    /// <summary>Unique identifier for this dead-letter record.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>The event that failed processing.</summary>
    public DomainEvent Event { get; init; } = null!;

    /// <summary>Name of the projection runner that failed.</summary>
    public string ProjectionName { get; init; } = string.Empty;

    /// <summary>Error message from the final failure attempt.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Number of attempts that were made before giving up.</summary>
    public int AttemptCount { get; init; }

    /// <summary>UTC timestamp when this entry was written to the dead-letter store.</summary>
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Whether this entry has been successfully reprocessed.</summary>
    public bool IsReprocessed { get; private set; }

    /// <summary>UTC timestamp when this entry was reprocessed, if applicable.</summary>
    public DateTime? ReprocessedAt { get; private set; }

    /// <summary>Marks the entry as successfully reprocessed.</summary>
    public void MarkReprocessed()
    {
        IsReprocessed = true;
        ReprocessedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Store for projection events that could not be processed after all retry
/// attempts were exhausted.  Entries can be queried for inspection and
/// requeued for reprocessing.
/// </summary>
public interface IDeadLetterStore
{
    /// <summary>Persists a failed event entry in the dead-letter store.</summary>
    Task WriteAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Returns all unprocessed entries for the given projection.</summary>
    Task<IReadOnlyList<DeadLetterEntry>> GetByProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all unprocessed entries for the given aggregate.</summary>
    Task<IReadOnlyList<DeadLetterEntry>> GetByAggregateAsync(
        string aggregateId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all entries (including reprocessed) with optional filtering.</summary>
    Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync(
        bool includeReprocessed = false,
        CancellationToken cancellationToken = default);

    /// <summary>Marks the entry with <paramref name="entryId"/> as successfully reprocessed.</summary>
    Task<Result> MarkReprocessedAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>Total number of dead-letter entries currently in the store.</summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
