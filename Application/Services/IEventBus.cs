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
/// Event bus interface for publishing and subscribing to domain events in an
/// event-sourced CQRS architecture. Supports both fire-and-forget publishing
/// and transactional publish-and-persist to an event store.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a single domain event to all registered handlers asynchronously.
    /// </summary>
    /// <param name="event">The domain event to publish.</param>
    /// <param name="cancellationToken">Token to cancel the publish operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of handler execution.</returns>
    Task<Result> PublishEventAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events sequentially in the order provided.
    /// All events are dispatched to their respective handlers.
    /// </summary>
    /// <param name="events">The ordered list of domain events to publish.</param>
    /// <param name="cancellationToken">Token to cancel the batch publish.</param>
    /// <returns>A <see cref="Result"/> indicating overall success or the first failure encountered.</returns>
    Task<Result> PublishEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an asynchronous handler for events of type <typeparamref name="TEvent"/>.
    /// The handler is invoked each time a matching event is published.
    /// </summary>
    /// <typeparam name="TEvent">The specific domain event type to subscribe to.</typeparam>
    /// <param name="handler">The async handler delegate invoked on event publication.</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;

    /// <summary>
    /// Removes a previously registered handler for events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler delegate to remove.</param>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent;

    /// <summary>
    /// Atomically persists a domain event to the event store and then publishes it
    /// to all handlers. If persistence fails, the event is not published.
    /// </summary>
    /// <param name="event">The domain event to persist and publish.</param>
    /// <param name="eventStore">The event store to persist the event to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> PublishAndPersistAsync(DomainEvent @event, IEventStore eventStore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Awaitable convenience wrapper over <see cref="Subscribe{TEvent}"/> for callers
    /// that prefer an asynchronous registration style. Registration itself is synchronous.
    /// </summary>
    /// <typeparam name="TEvent">The specific domain event type to subscribe to.</typeparam>
    /// <param name="handler">The async handler delegate invoked on event publication.</param>
    /// <returns>A completed task once the handler has been registered.</returns>
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        Subscribe(handler);
        return Task.CompletedTask;
    }
}
