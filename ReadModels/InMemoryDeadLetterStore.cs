#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IDeadLetterStore"/>.
/// Suitable for development and testing; replace with a durable store in production.
/// </summary>
public sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentDictionary<string, DeadLetterEntry> _entries = new();
    private readonly ILogger<InMemoryDeadLetterStore> _logger;

    /// <summary>Initialises the store with a required logger.</summary>
    public InMemoryDeadLetterStore(ILogger<InMemoryDeadLetterStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task WriteAsync(DeadLetterEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _logger.LogInformation("WriteAsync called with entry {EntryId}, projection {ProjectionName}", entry.Id, entry.ProjectionName);

        try
        {
            _logger.LogInformation("Writing dead-letter entry: id={EntryId}, projection={ProjectionName}", entry.Id, entry.ProjectionName);

            _entries[entry.Id] = entry;

            _logger.LogWarning(
                "Dead-letter entry written: projection={Projection}, eventId={EventId}, aggregateId={AggregateId}, attempts={Attempts}, error={Error}",
                entry.ProjectionName, entry.Event.EventId, entry.Event.AggregateId,
                entry.AttemptCount, entry.ErrorMessage);

            _logger.LogInformation("Finished writing dead-letter entry: id={EntryId}, projection={ProjectionName}", entry.Id, entry.ProjectionName);
            _logger.LogInformation("Finished writing dead-letter entry: id={EntryId}, projection={ProjectionName}", entry.Id, entry.ProjectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while writing dead-letter entry {EntryId}", entry.Id);
            throw;
        }

        _logger.LogInformation("WriteAsync completed for entry {EntryId}", entry.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetByProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetByProjectionAsync called with projectionName {ProjectionName}", projectionName);

        try
        {
            var results = _entries.Values
                .Where(e => !e.IsReprocessed && e.ProjectionName == projectionName)
                .OrderBy(e => e.FailedAt)
                .ToList();

            _logger.LogInformation("GetByProjectionAsync returning {Count} entries for projection {ProjectionName}", results.Count, projectionName);
            return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving dead-letter entries for projection {ProjectionName}", projectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetByAggregateAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetByAggregateAsync called with aggregateId {AggregateId}", aggregateId);

        try
        {
            var results = _entries.Values
                .Where(e => !e.IsReprocessed && e.Event.AggregateId == aggregateId)
                .OrderBy(e => e.FailedAt)
                .ToList();

            _logger.LogInformation("GetByAggregateAsync returning {Count} entries for aggregate {AggregateId}", results.Count, aggregateId);
            return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving dead-letter entries for aggregate {AggregateId}", aggregateId);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync(
        bool includeReprocessed = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAllAsync called with includeReprocessed {IncludeReprocessed}", includeReprocessed);

        try
        {
            var results = _entries.Values
                .Where(e => includeReprocessed || !e.IsReprocessed)
                .OrderBy(e => e.FailedAt)
                .ToList();

            _logger.LogInformation("GetAllAsync returning {Count} entries (includeReprocessed={IncludeReprocessed})", results.Count, includeReprocessed);
            return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving all dead-letter entries (includeReprocessed={IncludeReprocessed})", includeReprocessed);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<Result> MarkReprocessedAsync(string entryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MarkReprocessedAsync called with entryId {EntryId}", entryId);

        try
        {
            if (!_entries.TryGetValue(entryId, out var entry))
            {
                _logger.LogWarning("Dead-letter entry not found for reprocessing: {EntryId}", entryId);
                return Task.FromResult(Result.Failure("NOT_FOUND", $"Dead-letter entry '{entryId}' not found."));
            }

            entry.MarkReprocessed();

            _logger.LogInformation(
                "Dead-letter entry marked as reprocessed: id={EntryId}, projection={Projection}",
                entryId, entry.ProjectionName);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while marking dead-letter entry {EntryId} as reprocessed", entryId);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCountAsync called");

        try
        {
            var count = _entries.Values.Count(e => !e.IsReprocessed);
            _logger.LogInformation("GetCountAsync returning count {Count}", count);
            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while counting dead-letter entries");
            throw;
        }
    }
}
