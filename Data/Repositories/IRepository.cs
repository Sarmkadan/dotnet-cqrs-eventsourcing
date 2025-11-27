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
    Task<Result<T>> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result> SaveAsync(T aggregate, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<List<T>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
