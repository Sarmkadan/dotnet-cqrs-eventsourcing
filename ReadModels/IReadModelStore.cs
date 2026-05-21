#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Provides persistent, thread-safe storage for a specific read model type.
/// Each entry is addressed by a string key (typically the aggregate id).
/// </summary>
/// <typeparam name="TReadModel">The read model type stored in this store.</typeparam>
public interface IReadModelStore<TReadModel> where TReadModel : class
{
    /// <summary>Inserts or replaces the read model entry for the given key.</summary>
    Task<Result> UpsertAsync(string key, TReadModel model, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the read model entry for the given key.</summary>
    Task<Result<TReadModel>> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns all stored read model entries as an immutable snapshot.</summary>
    Task<Result<IReadOnlyList<TReadModel>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all entries that satisfy the supplied predicate.</summary>
    Task<Result<IReadOnlyList<TReadModel>>> QueryAsync(
        Func<TReadModel, bool> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the read model entry for the given key. Succeeds silently when the key is absent.</summary>
    Task<Result> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Returns the total number of entries currently held in the store.</summary>
    Task<Result<int>> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes all entries. Typically called at the start of a full projection rebuild.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Transforms a single domain event into an updated read model state.
/// Implementations are stateless; all context is passed in via parameters.
/// </summary>
/// <typeparam name="TReadModel">The read model type produced by this projector.</typeparam>
public interface IReadModelProjector<TReadModel> where TReadModel : class
{
    /// <summary>
    /// Logical name used for checkpointing and diagnostics.
    /// Should be stable across deployments (i.e. not derived from type names).
    /// </summary>
    string ProjectionName { get; }

    /// <summary>
    /// Returns <see langword="true"/> when this projector is able to handle <paramref name="event"/>.
    /// </summary>
    bool CanProject(DomainEvent @event);

    /// <summary>
    /// Derives the store key from an event. Typically returns <see cref="DomainEvent.AggregateId"/>.
    /// </summary>
    string GetKey(DomainEvent @event);

    /// <summary>
    /// Applies <paramref name="event"/> to <paramref name="current"/> and returns the new read model state.
    /// Return <see langword="null"/> to signal that the entry should be deleted from the store.
    /// </summary>
    Task<TReadModel?> ProjectAsync(
        DomainEvent @event,
        TReadModel? current,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Non-generic façade consumed by <see cref="ReadModelProjectionEngine"/>.
/// Hides the concrete read model type so the engine can work with a heterogeneous
/// collection of projectors without resorting to reflection.
/// </summary>
public interface IReadModelProjectionRunner
{
    /// <summary>Logical name of the underlying projection (forwarded from the projector).</summary>
    string ProjectionName { get; }

    /// <summary>Returns <see langword="true"/> when this runner can process <paramref name="event"/>.</summary>
    bool CanHandle(DomainEvent @event);

    /// <summary>
    /// Reads the current read model state, applies the event via the projector,
    /// and persists the result back to the store.
    /// </summary>
    Task<Result> RunAsync(DomainEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Couples a typed <see cref="IReadModelProjector{TReadModel}"/> with its
/// <see cref="IReadModelStore{TReadModel}"/> and exposes the pair as a single
/// <see cref="IReadModelProjectionRunner"/> for use by the engine.
/// </summary>
/// <typeparam name="TReadModel">The read model type managed by this runner.</typeparam>
public sealed class ReadModelProjectionRunner<TReadModel> : IReadModelProjectionRunner
    where TReadModel : class
{
    private readonly IReadModelProjector<TReadModel> _projector;
    private readonly IReadModelStore<TReadModel> _store;

    /// <summary>
    /// Initializes a new <see cref="ReadModelProjectionRunner{TReadModel}"/>.
    /// </summary>
    public ReadModelProjectionRunner(
        IReadModelProjector<TReadModel> projector,
        IReadModelStore<TReadModel> store)
    {
        _projector = GuardClauses.NotNull(projector, nameof(projector));
        _store = GuardClauses.NotNull(store, nameof(store));
    }

    /// <inheritdoc />
    public string ProjectionName => _projector.ProjectionName;

    /// <inheritdoc />
    public bool CanHandle(DomainEvent @event) => _projector.CanProject(@event);

    /// <inheritdoc />
    public async Task<Result> RunAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        var key = _projector.GetKey(@event);

        var fetchResult = await _store.GetAsync(key, cancellationToken).ConfigureAwait(false);
        var current = fetchResult.IsSuccess ? fetchResult.Data : null;

        var updated = await _projector.ProjectAsync(@event, current, cancellationToken).ConfigureAwait(false);

        return updated is null
            ? await _store.DeleteAsync(key, cancellationToken)
            : await _store.UpsertAsync(key, updated, cancellationToken).ConfigureAwait(false);
    }
}
