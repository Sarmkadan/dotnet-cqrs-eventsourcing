// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IReadModelStore{TReadModel}"/>
/// backed by a plain dictionary protected with a monitor lock.
/// Suitable for development, testing, and single-instance deployments.
/// Replace with a database-backed implementation for distributed scenarios.
/// </summary>
/// <typeparam name="TReadModel">The read model type stored in this store.</typeparam>
public sealed class InMemoryReadModelStore<TReadModel> : IReadModelStore<TReadModel>
    where TReadModel : class
{
    private readonly Dictionary<string, TReadModel> _store = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<Result> UpsertAsync(string key, TReadModel model, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));
        GuardClauses.NotNull(model, nameof(model));

        lock (_lock)
            _store[key] = model;

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<TReadModel>> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        lock (_lock)
        {
            return _store.TryGetValue(key, out var model)
                ? Task.FromResult(Result<TReadModel>.Success(model))
                : Task.FromResult(Result<TReadModel>.Failure(
                    "READ_MODEL_NOT_FOUND",
                    $"No read model entry found for key '{key}'."));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TReadModel>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TReadModel> snapshot;

        lock (_lock)
            snapshot = _store.Values.ToList();

        return Task.FromResult(Result<IReadOnlyList<TReadModel>>.Success(snapshot));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TReadModel>>> QueryAsync(
        Func<TReadModel, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNull(predicate, nameof(predicate));

        IReadOnlyList<TReadModel> matches;

        lock (_lock)
            matches = _store.Values.Where(predicate).ToList();

        return Task.FromResult(Result<IReadOnlyList<TReadModel>>.Success(matches));
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(key, nameof(key));

        lock (_lock)
            _store.Remove(key);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<int>> GetCountAsync(CancellationToken cancellationToken = default)
    {
        int count;

        lock (_lock)
            count = _store.Count;

        return Task.FromResult(Result<int>.Success(count));
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _store.Clear();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a diagnostic snapshot of all keys currently held in the store.
    /// Intended for testing and observability — not for production query paths.
    /// </summary>
    public IReadOnlyList<string> GetAllKeys()
    {
        lock (_lock)
            return _store.Keys.ToList();
    }
}
