// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Event store interface for saving and retrieving domain events with replay support.
/// </summary>
public interface IEventStore
{
    Task<Result> AppendEventAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    Task<Result> AppendEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default);
    Task<Result<List<DomainEvent>>> GetEventStreamAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default);
    Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result> ReplayEventsAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<List<DomainEvent>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<Result<int>> GetEventCountAsync(string aggregateId, CancellationToken cancellationToken = default);
}
