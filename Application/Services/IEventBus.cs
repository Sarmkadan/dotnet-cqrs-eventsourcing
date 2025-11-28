// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Event bus interface for publishing and subscribing to domain events.
/// </summary>
public interface IEventBus
{
    Task<Result> PublishEventAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    Task<Result> PublishEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default);
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;
    Task<Result> PublishAndPersistAsync(DomainEvent @event, IEventStore eventStore, CancellationToken cancellationToken = default);
}
