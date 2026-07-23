#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Results;
using Exceptions;

/// <summary>
/// In-memory event bus implementation for publishing and subscribing to domain events.
/// <para>
/// This implementation provides per-aggregate ordering guarantees: events with the same
/// <see cref="DomainEvent.AggregateId"/> are processed sequentially in the order they were published,
/// while events with different aggregate IDs can be processed in parallel.
/// </para>
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _subscribersLock = new();
    private readonly ILogger<EventBus> _logger;

    // Per-aggregate synchronization: ensures sequential processing for events with the same AggregateId
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _aggregateLocks = new();

    // Global lock for accessing _aggregateLocks to prevent race conditions during lock creation
    private readonly object _aggregateLocksSync = new();

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> PublishEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        return PublishEventsAsync(new List<DomainEvent> { @event }, cancellationToken);
    }

    public async Task<Result> PublishEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        if (events is null)
            throw new ArgumentNullException(nameof(events));

        try
        {
            foreach (var @event in events)
            {
                if (@event is null)
                    throw new ArgumentException("Event collection contains null element", nameof(events));

                await PublishSingleEventAsync(@event, cancellationToken);
            }

            _logger.LogInformation("Successfully published {EventCount} events", events.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing events");
            return Result.Failure("PUBLISH_FAILED", ex.Message);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);

        lock (_subscribersLock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        _logger.LogInformation("Subscribed handler for event type {EventType}", eventType.Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        bool removed;

        lock (_subscribersLock)
        {
            removed = _subscribers.TryGetValue(eventType, out var handlers) && handlers.Remove(handler);
        }

        if (removed)
        {
            _logger.LogInformation("Unsubscribed handler for event type {EventType}", eventType.Name);
        }
    }

    public async Task<Result> PublishAndPersistAsync(DomainEvent @event, IEventStore eventStore, CancellationToken cancellationToken = default)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));
        if (eventStore is null)
            throw new ArgumentNullException(nameof(eventStore));

        try
        {
            // Persist to event store first
            var persistResult = await eventStore.AppendEventAsync(@event, cancellationToken);
            if (!persistResult.IsSuccess)
                return persistResult;

            // Then publish
            return await PublishEventAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing and persisting event");
            return Result.Failure("PUBLISH_PERSIST_FAILED", ex.Message);
        }
    }

    private async Task PublishSingleEventAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        List<Delegate>? snapshot = null;

        lock (_subscribersLock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers) && handlers.Count > 0)
            {
                // Copy the handler list so concurrent Subscribe/Unsubscribe calls
                // cannot mutate it while the handlers are being invoked.
                snapshot = new List<Delegate>(handlers);
            }
        }

        if (snapshot is not null)
        {
            // Get or create a semaphore for this aggregate to ensure sequential processing
            var aggregateId = @event.AggregateId;
            SemaphoreSlim? aggregateLock = null;

            if (!string.IsNullOrEmpty(aggregateId))
            {
                // Use double-checked locking pattern to avoid unnecessary lock acquisition
                if (!_aggregateLocks.TryGetValue(aggregateId, out aggregateLock))
                {
                    lock (_aggregateLocksSync)
                    {
                        // Check again inside the lock to prevent race conditions
                        if (!_aggregateLocks.TryGetValue(aggregateId, out aggregateLock))
                        {
                            aggregateLock = new SemaphoreSlim(1, 1);
                            _aggregateLocks[aggregateId] = aggregateLock;
                        }
                    }
                }

                // Acquire the lock for this aggregate to ensure sequential processing
                await aggregateLock.WaitAsync(cancellationToken);
            }

            try
            {
                var tasks = snapshot.Select(async handler =>
                {
                    try
                    {
                        var method = handler.Method;
                        var parameters = method.GetParameters();

                        if (parameters.Length == 1 && parameters[0].ParameterType == eventType)
                        {
                            var result = handler.DynamicInvoke(@event);
                            if (result is Task task)
                            {
                                await task;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing handler for event type {EventType}", eventType.Name);
                    }
                });

                await Task.WhenAll(tasks);
            }
            finally
            {
                // Release the aggregate lock if we acquired one
                if (aggregateLock != null)
                {
                    aggregateLock.Release();

                    // Optional: Clean up the lock if it's no longer needed
                    // This helps prevent memory leaks from unused aggregate locks
                    // We could add a threshold or use WeakReference, but for now keep it simple
                }
            }
        }
    }
}
