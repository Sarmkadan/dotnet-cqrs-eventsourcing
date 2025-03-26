// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Shared.Results;

/// <summary>
/// Snapshot service interface for managing aggregate snapshots to optimize replay performance.
/// </summary>
public interface ISnapshotService
{
    Task<Result> CreateSnapshotAsync(string aggregateId, long version, string aggregateData, CancellationToken cancellationToken = default);
    Task<Result<(string AggregateData, long Version)>> GetLatestSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result> DeleteSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetSnapshotCountAsync(CancellationToken cancellationToken = default);
}
