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
    /// <summary>Unique identifier for this saga instance.</summary>
    string SagaId { get; }

    /// <summary>Human-readable name of the saga type (e.g., "FundTransferSaga").</summary>
    string SagaName { get; }

    /// <summary>Current lifecycle state of the saga.</summary>
    SagaState State { get; }

    /// <summary>UTC timestamp when the saga was created.</summary>
    DateTime StartedAt { get; }

    /// <summary>UTC timestamp of the last state transition, or null if not yet updated.</summary>
    DateTime? LastUpdatedAt { get; }

    /// <summary>Optional correlation identifier linking this saga to an originating command or context.</summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Domain events raised by the saga during processing that should be published
    /// to the event bus after the saga state is persisted.
    /// </summary>
    IReadOnlyList<DomainEvent> OutboxEvents { get; }

    /// <summary>Clears <see cref="OutboxEvents"/> once they have been dispatched.</summary>
    void ClearOutboxEvents();
}
