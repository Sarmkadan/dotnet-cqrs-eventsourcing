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
    Task<Result> SaveEventAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken = default);
    Task<Result> SaveEventsAsync(List<EventEnvelope> envelopes, CancellationToken cancellationToken = default);
    Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAndVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default);
    Task<Result<EventEnvelope>> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<Result<List<EventEnvelope>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<List<EventEnvelope>>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all events associated with the given partition key (e.g. tenant ID).
    /// This enables per-tenant replay, snapshot, and archival operations without
    /// scanning the full event stream. Returns an empty list when no events exist
    /// for the partition.
    /// </summary>
    Task<Result<List<EventEnvelope>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);
}
