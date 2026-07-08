#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Events;
using Shared.Results;
using Exceptions;

/// <summary>
/// Event store interface for saving and retrieving domain events with replay support.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends a single event to the store.
    /// </summary>
    /// <param name="event">The domain event.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AppendEventAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends multiple events to the store.
    /// </summary>
    /// <param name="events">The list of domain events.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AppendEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event stream for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the list of events.</returns>
    Task<Result<List<DomainEvent>>> GetEventStreamAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event stream from a specific version.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="fromVersion">The version to start from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the list of events.</returns>
    Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the version.</returns>
    Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays events for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ReplayEventsAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events by type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the list of events.</returns>
    Task<Result<List<DomainEvent>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event count for an aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the event count.</returns>
    Task<Result<int>> GetEventCountAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all domain events associated with the specified partition key (e.g. tenant ID),
    /// ordered by timestamp then aggregate version.  Supports paginated retrieval for large
    /// per-tenant streams.  Returns an empty list when no events exist for the partition.
    /// </summary>
    Task<Result<List<DomainEvent>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience wrapper over <see cref="GetEventStreamAsync"/> that returns the raw
    /// event list directly, yielding an empty list when the stream is missing or the
    /// underlying call fails.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The ordered list of events for the aggregate, or an empty list.</returns>
    async Task<List<DomainEvent>> GetEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var streamResult = await GetEventStreamAsync(aggregateId, cancellationToken);
        return streamResult.IsSuccess && streamResult.Data is not null
            ? streamResult.Data
            : new List<DomainEvent>();
    }
}
