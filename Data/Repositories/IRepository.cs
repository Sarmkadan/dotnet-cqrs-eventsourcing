#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Data.Repositories;

using Domain.AggregateRoots;
using Shared.Results;

/// <summary>
/// Generic repository interface for aggregate persistence and retrieval.
/// </summary>
public interface IRepository<T> where T : AggregateRoot
{
    /// <summary>
    /// Loads the aggregate with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the aggregate, or a failure if it cannot be found or loaded.</returns>
    Task<Result<T>> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the uncommitted changes of the specified aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the changes are persisted, or a failure otherwise.</returns>
    Task<Result> SaveAsync(T aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the aggregate with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the aggregate to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the deletion completes, or a failure otherwise.</returns>
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all aggregates of this type.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the list of aggregates, or a failure if retrieval fails.</returns>
    Task<Result<List<T>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an aggregate with the specified identifier exists.
    /// </summary>
    /// <param name="id">The aggregate identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the aggregate exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
