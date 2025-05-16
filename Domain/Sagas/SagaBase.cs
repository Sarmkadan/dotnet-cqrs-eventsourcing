#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Sagas;

using Events;

/// <summary>
/// Abstract base class for all saga implementations.  Provides lifecycle management,
/// outbox event collection, and helper methods to transition state.
/// </summary>
public abstract class SagaBase : ISaga
{
    private readonly List<DomainEvent> _outboxEvents = new();

    /// <inheritdoc/>
    public string SagaId { get; protected set; }

    /// <inheritdoc/>
    public abstract string SagaName { get; }

    /// <inheritdoc/>
    public SagaState State { get; protected set; }

    /// <inheritdoc/>
    public DateTime StartedAt { get; }

    /// <inheritdoc/>
    public DateTime? LastUpdatedAt { get; protected set; }

    /// <inheritdoc/>
    public string? CorrelationId { get; protected set; }

    /// <inheritdoc/>
    public IReadOnlyList<DomainEvent> OutboxEvents => _outboxEvents.AsReadOnly();

    protected SagaBase()
    {
        SagaId = Guid.NewGuid().ToString();
        State = SagaState.NotStarted;
        StartedAt = DateTime.UtcNow;
    }

    protected SagaBase(string sagaId) : this()
    {
        SagaId = sagaId;
    }

    /// <summary>
    /// Enqueues a domain event to be published after the saga state is persisted.
    /// </summary>
    protected void RaiseEvent(DomainEvent @event)
    {
        _outboxEvents.Add(@event);
    }

    /// <inheritdoc/>
    public void ClearOutboxEvents() => _outboxEvents.Clear();

    /// <summary>Transitions the saga to <see cref="SagaState.Active"/>.</summary>
    protected void Activate()
    {
        State = SagaState.Active;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions the saga to <see cref="SagaState.Completed"/>.</summary>
    protected void Complete()
    {
        State = SagaState.Completed;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the saga to <see cref="SagaState.Compensated"/> to signal
    /// that a rollback or compensating transaction has been triggered.
    /// </summary>
    protected void Compensate()
    {
        State = SagaState.Compensated;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions the saga to <see cref="SagaState.Failed"/>.</summary>
    protected void Fail()
    {
        State = SagaState.Failed;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public override string ToString()
        => $"{SagaName} {{ SagaId={SagaId}, State={State}, StartedAt={StartedAt} }}";
}
