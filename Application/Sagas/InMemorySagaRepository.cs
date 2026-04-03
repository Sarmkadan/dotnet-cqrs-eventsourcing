#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Sagas;

using System.Collections.Concurrent;
using Domain.Sagas;
using Infrastructure.Utilities;
using Shared.Results;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="ISagaRepository{TSaga}"/>.
/// Suitable for development, testing, and single-instance deployments.
/// </summary>
/// <typeparam name="TSaga">Concrete saga type managed by this repository.</typeparam>
public sealed class InMemorySagaRepository<TSaga> : ISagaRepository<TSaga>
    where TSaga : ISaga
{
    private readonly ConcurrentDictionary<string, TSaga> _store = new();

    /// <inheritdoc/>
    public Task<Result<TSaga>> GetByIdAsync(string sagaId, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(sagaId, nameof(sagaId));
        return _store.TryGetValue(sagaId, out var saga)
            ? Task.FromResult(Result<TSaga>.Success(saga))
            : Task.FromResult(Result<TSaga>.Failure("SAGA_NOT_FOUND", $"Saga '{sagaId}' not found."));
    }

    /// <inheritdoc/>
    public Task<Result<TSaga>> FindByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(correlationId, nameof(correlationId));
        var saga = _store.Values.FirstOrDefault(s => s.CorrelationId == correlationId);
        return saga is not null
            ? Task.FromResult(Result<TSaga>.Success(saga))
            : Task.FromResult(Result<TSaga>.Failure("SAGA_NOT_FOUND", $"No saga found for correlation '{correlationId}'."));
    }

    /// <inheritdoc/>
    public Task<Result> SaveAsync(TSaga saga, CancellationToken cancellationToken = default)
    {
        if (saga is null)
            return Task.FromResult(Result.Failure("NULL_SAGA", "Saga cannot be null."));
        _store[saga.SagaId] = saga;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<TSaga>>> GetAllAsync(SagaState? state = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<TSaga> sagas = _store.Values;
        if (state.HasValue)
            sagas = sagas.Where(s => s.State == state.Value);

        IReadOnlyList<TSaga> result = sagas.ToList();
        return Task.FromResult(Result<IReadOnlyList<TSaga>>.Success(result));
    }

    /// <inheritdoc/>
    public Task<Result> DeleteAsync(string sagaId, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(sagaId, nameof(sagaId));
        return _store.TryRemove(sagaId, out _)
            ? Task.FromResult(Result.Success())
            : Task.FromResult(Result.Failure("SAGA_NOT_FOUND", $"Saga '{sagaId}' not found."));
    }
}
