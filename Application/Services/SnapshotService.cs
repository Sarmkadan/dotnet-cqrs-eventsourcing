#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// Snapshot service implementation for storing and retrieving aggregate snapshots.
/// </summary>
public class SnapshotService : ISnapshotService
{
    private const string InvalidAggregateIdCode = "INVALID_AGGREGATE_ID";
    private const string InvalidAggregateIdMessage = "Aggregate ID cannot be empty";
    private const string InvalidVersionCode = "INVALID_VERSION";
    private const string InvalidVersionMessage = "Version must be greater than 0";
    private const string InvalidDataCode = "INVALID_DATA";
    private const string InvalidDataMessage = "Aggregate data cannot be empty";
    private const string CreateSnapshotFailedCode = "CREATE_SNAPSHOT_FAILED";
    private const string SnapshotNotFoundCode = "SNAPSHOT_NOT_FOUND";
    private const string SnapshotNotFoundMessage = "No snapshot found for aggregate {aggregateId}";
    private const string GetSnapshotFailedCode = "GET_SNAPSHOT_FAILED";
    private const string DeleteSnapshotFailedCode = "DELETE_SNAPSHOT_FAILED";
    private const string CheckSnapshotFailedCode = "CHECK_SNAPSHOT_FAILED";
    private const string GetCountFailedCode = "GET_COUNT_FAILED";
    private const string SnapshotSchemaMismatchCode = "SNAPSHOT_SCHEMA_MISMATCH";
    private const string SnapshotSchemaMismatchMessage = "Snapshot schema version mismatch for aggregate {aggregateId}";

    private readonly Dictionary<string, (string WrappedData, long Version, DateTime CreatedAt)> _snapshots = new();
    private readonly ILogger<SnapshotService> _logger;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotService"/> class.
    /// </summary>
    /// <param name="logger">Logger used to record snapshot activity.</param>
    public SnapshotService(ILogger<SnapshotService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Stores a snapshot for the specified aggregate in memory, replacing any previous snapshot.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier the snapshot belongs to.</param>
    /// <param name="version">The aggregate version the snapshot is taken at; must be greater than zero.</param>
    /// <param name="aggregateData">The serialized aggregate state; must not be null or whitespace.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the snapshot is stored, or a failure for invalid input.</returns>
    public Task<Result> CreateSnapshotAsync(string aggregateId, long version, string aggregateData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                return Task.FromResult(Result.Failure(InvalidAggregateIdCode, InvalidAggregateIdMessage));
            }

            if (version <= 0)
            {
                return Task.FromResult(Result.Failure(InvalidVersionCode, InvalidVersionMessage));
            }

            if (string.IsNullOrWhiteSpace(aggregateData))
            {
                return Task.FromResult(Result.Failure(InvalidDataCode, InvalidDataMessage));
            }

            // Wrap the aggregate data with schema version for storage
            var wrappedData = AggregateSnapshot.WrapForStorage(aggregateData);

            lock (_lockObject)
            {
                _snapshots[aggregateId] = (wrappedData, version, DateTime.UtcNow);
                _logger.LogInformation("Created snapshot for aggregate {AggregateId} at version {Version}", aggregateId, version);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating snapshot");
            return Task.FromResult(Result.Failure(CreateSnapshotFailedCode, ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the stored snapshot for the specified aggregate, validating its schema version.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the aggregate data and version, or a failure if no snapshot exists or its schema is outdated.</returns>
    public Task<Result<(string AggregateData, long Version)>> GetLatestSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                if (_snapshots.TryGetValue(aggregateId, out var snapshot))
                {
                    // Try to unwrap the data with current schema version
                    var aggregateData = AggregateSnapshot.TryUnwrapCurrentSchema(snapshot.WrappedData);
                    if (aggregateData == null)
                    {
                        _logger.LogWarning("Snapshot schema version mismatch for aggregate {AggregateId}. Discarding snapshot.", aggregateId);
                        return Task.FromResult(Result<(string, long)>.Failure(SnapshotSchemaMismatchCode, string.Format(SnapshotSchemaMismatchMessage, aggregateId)));
                    }

                    _logger.LogInformation("Retrieved snapshot for aggregate {AggregateId} at version {Version}", aggregateId, snapshot.Version);
                    return Task.FromResult(Result<(string, long)>.Success((aggregateData, snapshot.Version)));
                }
            }

            return Task.FromResult(Result<(string, long)>.Failure(SnapshotNotFoundCode, string.Format(SnapshotNotFoundMessage, aggregateId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving snapshot");
            return Task.FromResult(Result<(string, long)>.Failure(GetSnapshotFailedCode, ex.Message));
        }
    }

    /// <summary>
    /// Removes the stored snapshot for the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier whose snapshot should be removed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A success result when the snapshot is removed, or a failure if none exists.</returns>
    public Task<Result> DeleteSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                if (_snapshots.Remove(aggregateId))
                {
                    _logger.LogInformation("Deleted snapshot for aggregate {AggregateId}", aggregateId);
                    return Task.FromResult(Result.Success());
                }

                return Task.FromResult(Result.Failure(SnapshotNotFoundCode, string.Format(SnapshotNotFoundMessage, aggregateId)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting snapshot");
            return Task.FromResult(Result.Failure(DeleteSnapshotFailedCode, ex.Message));
        }
    }

    /// <summary>
    /// Checks whether a snapshot is stored for the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result whose value is <see langword="true"/> when a snapshot exists.</returns>
    public Task<Result<bool>> HasSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                var hasSnapshot = _snapshots.ContainsKey(aggregateId);
                return Task.FromResult(Result<bool>.Success(hasSnapshot));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking snapshot existence");
            return Task.FromResult(Result<bool>.Failure(CheckSnapshotFailedCode, ex.Message));
        }
    }

    /// <summary>
    /// Gets the number of snapshots currently stored in memory.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the number of stored snapshots.</returns>
    public Task<Result<int>> GetSnapshotCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                return Task.FromResult(Result<int>.Success(_snapshots.Count));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting snapshot count");
            return Task.FromResult(Result<int>.Failure(GetCountFailedCode, ex.Message));
        }
    }
}
