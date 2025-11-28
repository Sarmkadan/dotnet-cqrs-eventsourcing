// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// In-memory event bus implementation for publishing and subscribing to domain events.
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> PublishEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        return PublishEventsAsync(new List<DomainEvent> { @event }, cancellationToken);
    }

    public async Task<Result> PublishEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var @event in events)
            {
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
        var eventType = typeof(TEvent);

        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Delegate>();
        }

        _subscribers[eventType].Add(handler);
        _logger.LogInformation("Subscribed handler for event type {EventType}", eventType.Name);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);

        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            _logger.LogInformation("Unsubscribed handler for event type {EventType}", eventType.Name);
        }
    }

    public async Task<Result> PublishAndPersistAsync(DomainEvent @event, IEventStore eventStore, CancellationToken cancellationToken = default)
    {
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

        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers.Select(async handler =>
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
    }
}
