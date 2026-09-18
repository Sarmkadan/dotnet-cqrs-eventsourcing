#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Microsoft.Extensions.Logging;
using Shared.Constants;
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

    private readonly Dictionary<string, (string Data, long Version, DateTime CreatedAt)> _snapshots = new();
    private readonly ILogger<SnapshotService> _logger;
    private readonly object _lockObject = new();

    public SnapshotService(ILogger<SnapshotService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<Result> CreateSnapshotAsync(string aggregateId, long version, string aggregateData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                return Task.FromResult(Result.Failure(InvalidAggregateIdCode, InvalidAggregateIdMessage));

            if (version <= 0)
                return Task.FromResult(Result.Failure(InvalidVersionCode, InvalidVersionMessage));

            if (string.IsNullOrWhiteSpace(aggregateData))
                return Task.FromResult(Result.Failure(InvalidDataCode, InvalidDataMessage));

            lock (_lockObject)
            {
                _snapshots[aggregateId] = (aggregateData, version, DateTime.UtcNow);
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

    public Task<Result<(string AggregateData, long Version)>> GetLatestSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_lockObject)
            {
                if (_snapshots.TryGetValue(aggregateId, out var snapshot))
                {
                    _logger.LogInformation("Retrieved snapshot for aggregate {AggregateId} at version {Version}", aggregateId, snapshot.Version);
                    return Task.FromResult(Result<(string, long)>.Success((snapshot.Data, snapshot.Version)));
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
