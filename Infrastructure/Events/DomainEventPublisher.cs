// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Infrastructure.Events;

/// <summary>
/// In-process event publisher implementing Observer pattern for domain events.
/// Supports multiple subscribers per event type with handler error resilience.
/// Events are published synchronously; consider async pub-sub for high-throughput systems.
/// Registrations are thread-safe using ConcurrentDictionary.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// Collects errors from failing subscribers but continues publishing to others.
    /// </summary>
    Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple events in sequence.
    /// Useful for aggregate roots emitting multiple events from a single command.
    /// </summary>
    Task PublishManyAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes a handler function to a specific event type.
    /// Handler must not throw; exceptions are logged and caught.
    /// </summary>
    void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : DomainEvent;

    /// <summary>
    /// Unsubscribes a handler from an event type.
    /// </summary>
    void Unsubscribe<T>(Func<T, CancellationToken, Task> handler) where T : DomainEvent;
}

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscriptions = new();
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(ILogger<DomainEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNull(@event, nameof(@event));

        var eventType = @event.GetType();
        _logger.LogInformation(
            "Publishing domain event: {EventType} [AggregateId: {AggregateId}, CorrelationId: {CorrelationId}]",
            eventType.Name,
            @event.AggregateId,
            @event.CorrelationId
        );

        if (!_subscriptions.TryGetValue(eventType, out var handlers))
        {
            _logger.LogDebug("No subscribers for event type: {EventType}", eventType.Name);
            return;
        }

        var exceptions = new List<Exception>();

        foreach (var handler in handlers.ToList()) // ToList() to avoid modification during iteration
        {
            try
            {
                // Dynamically invoke handler with correct event type
                var method = handler.Method;
                var task = method.Invoke(handler.Target, new[] { @event, cancellationToken }) as Task;
                if (task is not null)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _logger.LogError(ex, "Error publishing event {EventType} to handler", eventType.Name);
            }
        }

        if (exceptions.Count > 0)
        {
            _logger.LogWarning(
                "Published event {EventType} with {ErrorCount} handler errors",
                eventType.Name,
                exceptions.Count
            );
        }
    }

    public async Task PublishManyAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();
        _logger.LogInformation("Publishing {Count} domain events", eventList.Count);

        foreach (var @event in eventList)
        {
            await PublishAsync(@event, cancellationToken);
        }
    }

    public void Subscribe<T>(Func<T, CancellationToken, Task> handler) where T : DomainEvent
    {
        GuardClauses.NotNull(handler, nameof(handler));

        var eventType = typeof(T);
        var handlers = _subscriptions.GetOrAdd(eventType, _ => new List<Delegate>());

        lock (handlers) // Lock list for thread-safe modifications
        {
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
                _logger.LogInformation("Subscriber registered for event type: {EventType}", eventType.Name);
            }
        }
    }

    public void Unsubscribe<T>(Func<T, CancellationToken, Task> handler) where T : DomainEvent
    {
        var eventType = typeof(T);

        if (_subscriptions.TryGetValue(eventType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
                _logger.LogInformation("Subscriber unregistered from event type: {EventType}", eventType.Name);
            }
        }
    }

    /// <summary>
    /// Gets subscriber count for a specific event type (useful for testing).
    /// </summary>
    public int GetSubscriberCount<T>() where T : DomainEvent
    {
        var eventType = typeof(T);
        return _subscriptions.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Clears all subscriptions. Useful in tests to prevent state leakage between test runs.
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
        _logger.LogInformation("All event subscriptions cleared");
    }
}
