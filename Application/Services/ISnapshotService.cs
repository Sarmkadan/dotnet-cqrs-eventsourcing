#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.AggregateRoots;
using Shared.Results;

/// <summary>
/// A retrieved aggregate snapshot together with the version it was captured at.
/// Provides <see cref="RestoreAggregate"/> to materialize the stored state back
/// into an <see cref="Account"/> aggregate.
/// </summary>
public sealed class AggregateSnapshot
{
    /// <summary>Serialized (JSON) aggregate state captured at snapshot time.</summary>
    public string AggregateData { get; }

    /// <summary>Aggregate version the snapshot was taken at.</summary>
    public long Version { get; }

    public AggregateSnapshot(string aggregateData, long version)
    {
        AggregateData = aggregateData ?? throw new ArgumentNullException(nameof(aggregateData));
        Version = version;
    }

    /// <summary>
    /// Deserializes the stored state back into an <see cref="Account"/> aggregate.
    /// </summary>
    public Account RestoreAggregate()
        => System.Text.Json.JsonSerializer.Deserialize<Account>(AggregateData) ?? new Account();
}

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

    /// <summary>
    /// Convenience overload that serializes the given aggregate to JSON and stores it
    /// via <see cref="CreateSnapshotAsync(string, long, string, CancellationToken)"/>.
    /// </summary>
    /// <param name="aggregate">The aggregate instance to snapshot.</param>
    /// <param name="aggregateId">The aggregate identifier the snapshot belongs to.</param>
    /// <param name="version">The aggregate version the snapshot is taken at.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<Result> CreateSnapshotAsync(AggregateRoot aggregate, string aggregateId, long version, CancellationToken cancellationToken = default)
    {
        var aggregateData = System.Text.Json.JsonSerializer.Serialize(aggregate, aggregate.GetType());
        return CreateSnapshotAsync(aggregateId, version, aggregateData, cancellationToken);
    }

    /// <summary>
    /// Convenience wrapper over <see cref="GetLatestSnapshotAsync"/> returning a restorable
    /// snapshot handle, or <see langword="null"/> when no snapshot exists.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    async Task<AggregateSnapshot?> GetSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        var snapshotResult = await GetLatestSnapshotAsync(aggregateId, cancellationToken);
        return snapshotResult.IsSuccess
            ? new AggregateSnapshot(snapshotResult.Data.AggregateData, snapshotResult.Data.Version)
            : null;
    }
}
