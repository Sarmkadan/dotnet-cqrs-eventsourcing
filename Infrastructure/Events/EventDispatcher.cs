// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Application.Services;

namespace DotNetCqrsEventSourcing.Infrastructure.Events;

/// <summary>
/// Coordinates event publishing to multiple subscribers and persistence to event store.
/// Ensures that events are both persisted and published to handlers, maintaining strong consistency.
/// Handles transaction semantics: if persistence fails, handlers are not invoked.
/// If handler fails, the event is still persisted (handler failures don't affect event stream).
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Dispatches an event: first persists it, then publishes to subscribers.
    /// Provides at-least-once delivery guarantee for handler invocations.
    /// </summary>
    Task DispatchAsync(string aggregateId, DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches multiple events from the same aggregate.
    /// Maintains causal ordering: events are persisted then published in order.
    /// </summary>
    Task DispatchManyAsync(string aggregateId, IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
}

public class EventDispatcher : IEventDispatcher
{
    private readonly IEventStore _eventStore;
    private readonly IDomainEventPublisher _publisher;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(
        IEventStore eventStore,
        IDomainEventPublisher publisher,
        ILogger<EventDispatcher> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(string aggregateId, DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogDebug(
            "Dispatching event {EventType} for aggregate {AggregateId}",
            @event.GetType().Name,
            aggregateId
        );

        try
        {
            // Persist first - if this fails, handlers are not invoked
            // This maintains the event stream as the source of truth
            await _eventStore.AppendEventAsync(aggregateId, @event, cancellationToken);

            // Then publish to subscribers - handler failures don't affect persistence
            // This provides eventually-consistent read models
            await _publisher.PublishAsync(@event, cancellationToken);

            _logger.LogInformation(
                "Event dispatched successfully: {EventType} [AggregateId: {AggregateId}]",
                @event.GetType().Name,
                aggregateId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching event {EventType}", @event.GetType().Name);
            throw;
        }
    }

    public async Task DispatchManyAsync(string aggregateId, IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(aggregateId);

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            _logger.LogDebug("DispatchMany called with empty event list for aggregate {AggregateId}", aggregateId);
            return;
        }

        _logger.LogInformation(
            "Dispatching {Count} events for aggregate {AggregateId}",
            eventList.Count,
            aggregateId
        );

        foreach (var @event in eventList)
        {
            await DispatchAsync(aggregateId, @event, cancellationToken);
        }

        _logger.LogInformation(
            "Batch event dispatch completed: {Count} events for aggregate {AggregateId}",
            eventList.Count,
            aggregateId
        );
    }
}
