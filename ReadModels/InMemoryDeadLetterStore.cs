#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
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

        _entries[entry.Id] = entry;

        _logger.LogWarning(
            "Dead-letter entry written: projection={Projection}, eventId={EventId}, aggregateId={AggregateId}, attempts={Attempts}, error={Error}",
            entry.ProjectionName, entry.Event.EventId, entry.Event.AggregateId,
            entry.AttemptCount, entry.ErrorMessage);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetByProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => !e.IsReprocessed && e.ProjectionName == projectionName)
            .OrderBy(e => e.FailedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetByAggregateAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => !e.IsReprocessed && e.Event.AggregateId == aggregateId)
            .OrderBy(e => e.FailedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync(
        bool includeReprocessed = false,
        CancellationToken cancellationToken = default)
    {
        var results = _entries.Values
            .Where(e => includeReprocessed || !e.IsReprocessed)
            .OrderBy(e => e.FailedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<DeadLetterEntry>>(results);
    }

    /// <inheritdoc />
    public Task<Result> MarkReprocessedAsync(string entryId, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
            return Task.FromResult(Result.Failure("NOT_FOUND", $"Dead-letter entry '{entryId}' not found."));

        entry.MarkReprocessed();

        _logger.LogInformation(
            "Dead-letter entry marked as reprocessed: id={EntryId}, projection={Projection}",
            entryId, entry.ProjectionName);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_entries.Values.Count(e => !e.IsReprocessed));
}
