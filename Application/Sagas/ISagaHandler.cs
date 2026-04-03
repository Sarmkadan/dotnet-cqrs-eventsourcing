#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Sagas;

using Domain.Events;
using Domain.Sagas;
using Shared.Results;

/// <summary>
/// Handles a specific domain event for a saga type, responsible for loading or
/// creating the saga instance and applying the event to its state.
/// </summary>
/// <typeparam name="TSaga">Concrete saga type this handler manages.</typeparam>
/// <typeparam name="TEvent">The domain event type that triggers this handler.</typeparam>
public interface ISagaHandler<TSaga, TEvent>
    where TSaga : ISaga
    where TEvent : DomainEvent
{
    /// <summary>
    /// Handles the domain event by loading the appropriate saga instance (or creating
    /// a new one), applying state transitions, and persisting the result via the
    /// <see cref="ISagaRepository{TSaga}"/>.
    /// </summary>
    /// <param name="event">The domain event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> HandleAsync(TEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> when this handler can process <paramref name="event"/>.
    /// The default implementation checks the runtime type; override for correlation-based routing.
    /// </summary>
    bool CanHandle(DomainEvent @event) => @event is TEvent;
}
