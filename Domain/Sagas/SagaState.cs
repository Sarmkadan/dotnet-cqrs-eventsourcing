#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Sagas;

/// <summary>
/// Lifecycle states for a saga instance.
/// </summary>
public enum SagaState
{
    /// <summary>The saga has been created but has not yet processed any events.</summary>
    NotStarted,

    /// <summary>The saga is actively processing events and awaiting further inputs.</summary>
    Active,

    /// <summary>The saga completed successfully and no further events will be handled.</summary>
    Completed,

    /// <summary>
    /// The saga was explicitly cancelled or a compensating transaction was triggered.
    /// </summary>
    Compensated,

    /// <summary>The saga encountered an unrecoverable error and is in a failed terminal state.</summary>
    Failed
}
