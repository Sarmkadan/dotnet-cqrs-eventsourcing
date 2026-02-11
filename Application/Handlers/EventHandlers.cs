// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Handlers;

using Domain.Events;
using Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Event handlers for domain events - extensible handler pattern.
/// </summary>
public class EventHandlers
{
    private readonly IEventBus _eventBus;
    private readonly IProjectionService _projectionService;
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<EventHandlers> _logger;

    public EventHandlers(
        IEventBus eventBus,
        IProjectionService projectionService,
        ISnapshotService snapshotService,
        ILogger<EventHandlers> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register all event handlers.
    /// </summary>
    public void RegisterHandlers()
    {
        // Account created handler
        _eventBus.Subscribe<AccountCreatedEvent>(HandleAccountCreated);

        // Money events handlers
        _eventBus.Subscribe<MoneyDepositedEvent>(HandleMoneyDeposited);
        _eventBus.Subscribe<MoneyWithdrawnEvent>(HandleMoneyWithdrawn);

        // Account lifecycle handlers
        _eventBus.Subscribe<AccountClosedEvent>(HandleAccountClosed);

        // Projection update handlers
        _eventBus.Subscribe<DomainEvent>(HandleProjectionUpdate);

        _logger.LogInformation("Event handlers registered successfully");
    }

    // Handler implementations

    private async Task HandleAccountCreated(AccountCreatedEvent @event)
    {
        _logger.LogInformation(
            "Handling AccountCreated: {AccountNumber} for {AccountHolder}",
            @event.AccountNumber,
            @event.AccountHolder
        );

        // Update projections
        await _projectionService.UpdateProjectionAsync(@event);

        _logger.LogInformation("AccountCreated event handled");
    }

    private async Task HandleMoneyDeposited(MoneyDepositedEvent @event)
    {
        _logger.LogInformation(
            "Handling MoneyDeposited: {Amount} deposited",
            @event.Amount
        );

        // Update projections
        await _projectionService.UpdateProjectionAsync(@event);

        _logger.LogInformation("MoneyDeposited event handled");
    }

    private async Task HandleMoneyWithdrawn(MoneyWithdrawnEvent @event)
    {
        _logger.LogInformation(
            "Handling MoneyWithdrawn: {Amount} withdrawn",
            @event.Amount
        );

        // Update projections
        await _projectionService.UpdateProjectionAsync(@event);

        _logger.LogInformation("MoneyWithdrawn event handled");
    }

    private async Task HandleAccountClosed(AccountClosedEvent @event)
    {
        _logger.LogInformation(
            "Handling AccountClosed: Final Balance {Balance}",
            @event.ClosingBalance
        );

        // Update projections
        await _projectionService.UpdateProjectionAsync(@event);

        // Create final snapshot
        var snapshotData = $"{{\"status\":\"closed\",\"closingBalance\":{@event.ClosingBalance},\"closedAt\":\"{@event.OccurredAt:O}\"}}";
        await _snapshotService.CreateSnapshotAsync(@event.AggregateId, @event.AggregateVersion, snapshotData);

        _logger.LogInformation("AccountClosed event handled - snapshot created");
    }

    private async Task HandleProjectionUpdate(DomainEvent @event)
    {
        // This is a catch-all for updating projections on all events
        await _projectionService.UpdateProjectionAsync(@event);
    }
}

/// <summary>
/// Async event handler wrapper for custom handlers.
/// </summary>
public abstract class EventHandler<TEvent> where TEvent : DomainEvent
{
    protected ILogger Logger { get; }

    protected EventHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handle the domain event.
    /// </summary>
    public abstract Task HandleAsync(TEvent @event);

    /// <summary>
    /// Handle any errors during event processing.
    /// </summary>
    public virtual Task HandleErrorAsync(TEvent @event, Exception exception)
    {
        Logger.LogError(exception, "Error handling event: {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Saga coordinator for handling complex, long-running transaction patterns.
/// </summary>
public abstract class EventSaga
{
    protected IEventBus EventBus { get; }
    protected ILogger Logger { get; }

    protected EventSaga(IEventBus eventBus, ILogger logger)
    {
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start saga processing.
    /// </summary>
    public abstract Task StartAsync(DomainEvent triggeringEvent);

    /// <summary>
    /// Handle saga step completion.
    /// </summary>
    public abstract Task CompleteStepAsync(DomainEvent @event);

    /// <summary>
    /// Handle saga compensation (rollback).
    /// </summary>
    public abstract Task CompensateAsync(string sagaId);
}
