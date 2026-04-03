#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Sagas;

using Domain.Sagas;
using Shared.Results;

/// <summary>
/// Persistence contract for saga instances.  Implementations must be safe for
/// concurrent access and should handle optimistic concurrency where applicable.
/// </summary>
/// <typeparam name="TSaga">Concrete saga type persisted by this repository.</typeparam>
public interface ISagaRepository<TSaga> where TSaga : ISaga
{
    /// <summary>Retrieves a saga instance by its identifier.</summary>
    Task<Result<TSaga>> GetByIdAsync(string sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the first saga instance associated with the given correlation identifier,
    /// or returns a not-found failure when none exists.
    /// </summary>
    Task<Result<TSaga>> FindByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new or updated saga instance.</summary>
    Task<Result> SaveAsync(TSaga saga, CancellationToken cancellationToken = default);

    /// <summary>Returns all saga instances, optionally filtered by <paramref name="state"/>.</summary>
    Task<Result<IReadOnlyList<TSaga>>> GetAllAsync(SagaState? state = null, CancellationToken cancellationToken = default);

    /// <summary>Removes the saga instance from the store.</summary>
    Task<Result> DeleteAsync(string sagaId, CancellationToken cancellationToken = default);
}
