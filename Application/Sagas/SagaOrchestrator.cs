#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Sagas;

using Domain.Events;
using Domain.Sagas;
using Infrastructure.Utilities;
using Microsoft.Extensions.Logging;
using Services;
using Shared.Results;

/// <summary>
/// Routes domain events to all registered saga handlers, publishes outbox events
/// produced by sagas, and logs lifecycle transitions.
/// <para>
/// Register saga handlers as <c>ISagaHandlerWrapper</c> in the DI container via
/// <see cref="SagaHandlerWrapper{TSaga,TEvent}"/>, then subscribe this orchestrator
/// to the event bus.
/// </para>
/// </summary>
public sealed class SagaOrchestrator
{
    private readonly IReadOnlyList<ISagaHandlerWrapper> _handlers;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(
        IEnumerable<ISagaHandlerWrapper> handlers,
        IEventBus eventBus,
        ILogger<SagaOrchestrator> logger)
    {
        _handlers = GuardClauses.NotNull(handlers, nameof(handlers)).ToList();
        _eventBus = GuardClauses.NotNull(eventBus, nameof(eventBus));
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <summary>
    /// Dispatches <paramref name="event"/> to every handler that can process it,
    /// then publishes any outbox events emitted by those sagas.
    /// </summary>
    public async Task<Result> DispatchAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        var capable = _handlers.Where(h => h.CanHandle(@event)).ToList();
        if (capable.Count == 0)
            return Result.Success();

        _logger.LogInformation(
            "Dispatching {EventType} to {HandlerCount} saga handler(s).",
            @event.GetEventType(), capable.Count);

        var errors = new List<string>();

        foreach (var handler in capable)
        {
            try
            {
                var result = await handler.HandleAsync(@event, cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.LogError(
                        "Saga handler failed for event {EventType}: {Error}",
                        @event.GetEventType(), result.ErrorMessage);
                    errors.Add(result.ErrorMessage!);
                }

                // Publish any events the saga produced
                foreach (var outboxEvent in handler.DrainOutboxEvents())
                {
                    await _eventBus.PublishEventAsync(outboxEvent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in saga handler for event {EventType}", @event.GetEventType());
                errors.Add(ex.Message);
            }
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure("SAGA_DISPATCH_ERROR", string.Join("; ", errors));
    }
}

/// <summary>
/// Non-generic wrapper used by <see cref="SagaOrchestrator"/> to avoid generic
/// type proliferation at the dispatch level.
/// </summary>
public interface ISagaHandlerWrapper
{
    /// <summary>Returns <see langword="true"/> when this handler can process the event.</summary>
    bool CanHandle(DomainEvent @event);

    /// <summary>Applies the event to the relevant saga instance.</summary>
    Task<Result> HandleAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>Returns and clears all outbox events produced by the last <see cref="HandleAsync"/> call.</summary>
    IReadOnlyList<DomainEvent> DrainOutboxEvents();
}

/// <summary>
/// Generic adapter that bridges a strongly-typed <see cref="ISagaHandler{TSaga,TEvent}"/>
/// to the <see cref="ISagaHandlerWrapper"/> contract consumed by <see cref="SagaOrchestrator"/>.
/// </summary>
/// <typeparam name="TSaga">Saga type.</typeparam>
/// <typeparam name="TEvent">Domain event type the inner handler processes.</typeparam>
public sealed class SagaHandlerWrapper<TSaga, TEvent> : ISagaHandlerWrapper
    where TSaga : SagaBase
    where TEvent : DomainEvent
{
    private readonly ISagaHandler<TSaga, TEvent> _inner;
    private List<DomainEvent> _latestOutbox = new();

    public SagaHandlerWrapper(ISagaHandler<TSaga, TEvent> inner)
    {
        _inner = GuardClauses.NotNull(inner, nameof(inner));
    }

    /// <inheritdoc/>
    public bool CanHandle(DomainEvent @event) => @event is TEvent;

    /// <inheritdoc/>
    public async Task<Result> HandleAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is not TEvent typed)
            return Result.Failure("WRONG_EVENT_TYPE", $"Expected {typeof(TEvent).Name} but got {@event.GetType().Name}.");

        return await _inner.HandleAsync(typed, cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainEvent> DrainOutboxEvents()
    {
        var events = _latestOutbox;
        _latestOutbox = new List<DomainEvent>();
        return events;
    }
}
