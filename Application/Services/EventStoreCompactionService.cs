#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Data.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// Default implementation of <see cref="IEventStoreCompactionService"/>.
/// Delegates deletion to <see cref="IEventRepository"/> and uses
/// <see cref="ISnapshotService"/> to discover the safe cut-off version.
/// </summary>
public sealed class EventStoreCompactionService : IEventStoreCompactionService
{
    private const string NoSnapshotErrorCode = "NO_SNAPSHOT";
    private const string InvalidVersionErrorCode = "INVALID_VERSION";
    private const string CompactionFailedErrorCode = "COMPACTION_FAILED";
    private const long MinimumKeepFromVersion = 0;

    private readonly IEventRepository _eventRepository;
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<EventStoreCompactionService> _logger;

    public EventStoreCompactionService(
        IEventRepository eventRepository,
        ISnapshotService snapshotService,
        ILogger<EventStoreCompactionService> logger)
    {
        ArgumentNullException.ThrowIfNull(eventRepository);
        ArgumentNullException.ThrowIfNull(snapshotService);
        ArgumentNullException.ThrowIfNull(logger);

        _eventRepository = eventRepository;
        _snapshotService = snapshotService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<CompactionResult>> CompactAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        var snapshotResult = await _snapshotService.GetLatestSnapshotAsync(aggregateId, cancellationToken);
        if (!snapshotResult.IsSuccess)
        {
            _logger.LogWarning(
                "Skipping compaction for aggregate {AggregateId}: no snapshot found ({Code})",
                aggregateId, snapshotResult.ErrorCode);
            return Result<CompactionResult>.Failure(
                NoSnapshotErrorCode,
                $"Cannot compact aggregate '{aggregateId}' without a snapshot. Create a snapshot first.");
        }

        var snapshotVersion = snapshotResult.Data!.Version;
        return await CompactToVersionAsync(aggregateId, snapshotVersion, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<CompactionResult>> CompactToVersionAsync(
        string aggregateId,
        long keepFromVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        if (keepFromVersion <= MinimumKeepFromVersion)
            return Result<CompactionResult>.Failure(
                InvalidVersionErrorCode,
                "keepFromVersion must be greater than zero.");

        _logger.LogInformation(
            "Compacting event stream for aggregate {AggregateId} – deleting events before version {Version}.",
            aggregateId, keepFromVersion);

        try
        {
            var deleteResult = await _eventRepository.DeleteEventsBeforeVersionAsync(
                aggregateId, keepFromVersion, cancellationToken);

            if (!deleteResult.IsSuccess)
                return Result<CompactionResult>.Failure(deleteResult.ErrorCode!, deleteResult.ErrorMessage!);

            var compaction = new CompactionResult(
                aggregateId,
                deleteResult.Data!,
                keepFromVersion,
                DateTime.UtcNow);

            _logger.LogInformation(
                "Compaction completed for aggregate {AggregateId}: {Removed} event(s) deleted, keeping from v{Version}.",
                aggregateId, compaction.EventsRemoved, keepFromVersion);

            return Result<CompactionResult>.Success(compaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during compaction for aggregate {AggregateId}", aggregateId);
            return Result<CompactionResult>.Failure(CompactionFailedErrorCode, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CompactionResult>>> CompactAllAsync(
        IEnumerable<string> aggregateIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateIds);

        var results = new List<CompactionResult>();

        foreach (var id in aggregateIds)
        {
            var result = await CompactAsync(id, cancellationToken);
            if (result.IsSuccess)
                results.Add(result.Data!);
            // Aggregates without snapshots are silently skipped (CompactAsync already logs a warning).
        }

        _logger.LogInformation(
            "Bulk compaction finished: {Count} aggregate(s) compacted.",
            results.Count);

        return Result<IReadOnlyList<CompactionResult>>.Success(results);
    }
}
