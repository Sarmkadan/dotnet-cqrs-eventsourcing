#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Sagas;

using Events;

/// <summary>
/// Represents a long-running process that coordinates actions across multiple aggregates
/// in response to domain events.  A saga reacts to events, maintains its own state, and
/// may raise further domain events to drive the workflow forward.
/// </summary>
public interface ISaga
{
    /// <summary>
    /// Gets the unique identifier of this saga instance.
    /// </summary>
    /// <value>The identifier used to persist and retrieve the saga.</value>
    string SagaId { get; }

    /// <summary>
    /// Gets the human-readable name of the saga type.
    /// </summary>
    /// <value>A name that identifies the saga type, such as <c>FundTransferSaga</c>.</value>
    string SagaName { get; }

    /// <summary>
    /// Gets the current lifecycle state of the saga.
    /// </summary>
    /// <value>The saga's current <see cref="SagaState"/>.</value>
    SagaState State { get; }

    /// <summary>
    /// Gets the UTC date and time when the saga was created.
    /// </summary>
    /// <value>The saga creation timestamp in UTC.</value>
    DateTime StartedAt { get; }

    /// <summary>
    /// Gets the UTC date and time of the saga's most recent state transition.
    /// </summary>
    /// <value>
    /// The most recent transition timestamp in UTC, or <see langword="null"/> if the saga has not
    /// transitioned since it was created.
    /// </value>
    DateTime? LastUpdatedAt { get; }

    /// <summary>
    /// Gets the correlation identifier that links the saga to its originating command or context.
    /// </summary>
    /// <value>The correlation identifier, or <see langword="null"/> when none was provided.</value>
    string? CorrelationId { get; }

    /// <summary>
    /// Domain events raised by the saga during processing that should be published
    /// to the event bus after the saga state is persisted.
    /// </summary>
    /// <value>A read-only collection of domain events awaiting publication.</value>
    IReadOnlyList<DomainEvent> OutboxEvents { get; }

    /// <summary>
    /// Removes all domain events from <see cref="OutboxEvents"/> after they have been dispatched.
    /// </summary>
    void ClearOutboxEvents();
}
