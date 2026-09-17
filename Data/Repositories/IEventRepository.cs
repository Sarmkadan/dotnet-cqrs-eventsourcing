#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Repository interface for persisting and retrieving domain events.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Persists a single domain event envelope to the event store.
    /// </summary>
    /// <param name="eventEnvelope">The event envelope to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating whether the event was saved successfully.</returns>
    Task<Result> SaveEventAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a batch of domain event envelopes to the event store.
    /// </summary>
    /// <param name="envelopes">The event envelopes to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating whether the events were saved successfully.</returns>
    Task<Result> SaveEventsAsync(List<EventEnvelope> envelopes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events for the specified aggregate, ordered by version.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the aggregate's events, or an empty list when none exist.</returns>
    Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves events for the specified aggregate whose version is greater than or equal to
    /// <paramref name="fromVersion"/>. Used for incremental replay from a known version.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="fromVersion">The inclusive lower-bound version to start from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the matching events, or an empty list when none exist.</returns>
    Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAndVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single event envelope by its unique identifier.
    /// </summary>
    /// <param name="eventId">The identifier of the event.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the event, or a failure when it cannot be found.</returns>
    Task<Result<EventEnvelope>> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events of the specified type.
    /// </summary>
    /// <param name="eventType">The event type to filter by.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the matching events, or an empty list when none exist.</returns>
    Task<Result<List<EventEnvelope>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current version of the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the aggregate's current version.</returns>
    Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events across the event store, paged.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The number of events per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the requested page of events.</returns>
    Task<Result<List<EventEnvelope>>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all events associated with the given partition key (e.g. tenant ID).
    /// This enables per-tenant replay, snapshot, and archival operations without
    /// scanning the full event stream. Returns an empty list when no events exist
    /// for the partition.
    /// </summary>
    Task<Result<List<EventEnvelope>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all events for the specified aggregate whose version is strictly less than
    /// <paramref name="beforeVersion"/>.  Used by the compaction service to prune
    /// superseded events once a snapshot has been captured at that version.
    /// Returns the number of events that were removed.
    /// </summary>
    Task<Result<int>> DeleteEventsBeforeVersionAsync(string aggregateId, long beforeVersion, CancellationToken cancellationToken = default);
}
