#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Outcome of a bulk dead-letter replay run, capturing which entries were
/// successfully re-dispatched and which were skipped or failed, with reasons.
/// </summary>
/// <param name="Succeeded">Identifiers of entries that were re-dispatched and marked resolved.</param>
/// <param name="Failed">Identifiers paired with the reason replay did not succeed.</param>
public sealed record DeadLetterReplayBatchResult(
    IReadOnlyList<string> Succeeded,
    IReadOnlyList<(string EntryId, string Reason)> Failed);

/// <summary>
/// Re-dispatches dead-lettered events back to their originating projection runner,
/// marking them resolved on success. Enforces per-stream ordering: an entry cannot
/// be replayed while a later event for the same aggregate stream has already been
/// successfully applied to the same projection, since that would let the projection
/// regress past state it has already moved beyond.
/// </summary>
public sealed class DeadLetterReplayService
{
    private readonly IDeadLetterStore _deadLetterStore;
    private readonly IEventStore _eventStore;
    private readonly IReadOnlyList<IReadModelProjectionRunner> _runners;
    private readonly ILogger<DeadLetterReplayService> _logger;

    /// <summary>Initializes the service with the stores and projection runners it coordinates.</summary>
    /// <exception cref="ArgumentNullException">Any constructor argument is <see langword="null"/>.</exception>
    public DeadLetterReplayService(
        IDeadLetterStore deadLetterStore,
        IEventStore eventStore,
        IEnumerable<IReadModelProjectionRunner> runners,
        ILogger<DeadLetterReplayService> logger)
    {
        _deadLetterStore = GuardClauses.NotNull(deadLetterStore, nameof(deadLetterStore));
        _eventStore = GuardClauses.NotNull(eventStore, nameof(eventStore));
        _runners = GuardClauses.NotNull(runners, nameof(runners)).ToList();
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <summary>
    /// Replays a single dead-letter entry by re-dispatching its event to the projection
    /// runner that originally failed to process it, then marks the entry resolved.
    /// </summary>
    /// <param name="entryId">Identifier of the dead-letter entry to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> when the event was re-applied and the entry marked
    /// resolved; otherwise a failure describing why replay was refused or the projector failed.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="entryId"/> is null or empty.</exception>
    public async Task<Result> ReplayAsync(string entryId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entryId);

        var allEntries = await _deadLetterStore.GetAllAsync(includeReprocessed: true, cancellationToken);
        var entry = allEntries.FirstOrDefault(e => e.Id == entryId);

        if (entry is null)
            return Result.Failure("NOT_FOUND", $"Dead-letter entry '{entryId}' not found.");

        if (entry.IsReprocessed)
            return Result.Failure("ALREADY_REPROCESSED", $"Dead-letter entry '{entryId}' was already reprocessed.");

        var orderingResult = await CheckOrderingAsync(entry, cancellationToken);
        if (!orderingResult.IsSuccess)
            return orderingResult;

        var runner = _runners.FirstOrDefault(r => r.ProjectionName == entry.ProjectionName);
        if (runner is null)
            return Result.Failure(
                "PROJECTION_NOT_FOUND",
                $"No registered projection runner named '{entry.ProjectionName}' is available to replay this entry.");

        var runResult = await runner.RunAsync(entry.Event, cancellationToken);
        if (!runResult.IsSuccess)
        {
            _logger.LogWarning(
                "Dead-letter replay failed for entry {EntryId}: {Error}",
                entryId, runResult.ErrorMessage);

            return Result.Failure(
                "REPLAY_FAILED",
                runResult.ErrorMessage ?? "Projection runner rejected the replayed event.");
        }

        var markResult = await _deadLetterStore.MarkReprocessedAsync(entryId, cancellationToken);

        _logger.LogInformation(
            "Dead-letter entry {EntryId} replayed successfully for projection {Projection}.",
            entryId, entry.ProjectionName);

        return markResult;
    }

    /// <summary>
    /// Replays every eligible, unresolved dead-letter entry, optionally scoped to a single
    /// projection. Entries are processed oldest-first per aggregate stream so that ordering
    /// constraints are satisfied naturally as earlier entries resolve.
    /// </summary>
    /// <param name="projectionName">
    /// When supplied, only entries for this projection are replayed; otherwise all
    /// unresolved entries across every projection are attempted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary of which entries succeeded and which were skipped or failed, with reasons.</returns>
    public async Task<Result<DeadLetterReplayBatchResult>> ReplayAllAsync(
        string? projectionName = null,
        CancellationToken cancellationToken = default)
    {
        var entries = string.IsNullOrWhiteSpace(projectionName)
            ? await _deadLetterStore.GetAllAsync(includeReprocessed: false, cancellationToken)
            : await _deadLetterStore.GetByProjectionAsync(projectionName, cancellationToken);

        var ordered = entries
            .OrderBy(e => e.Event.AggregateId, StringComparer.Ordinal)
            .ThenBy(e => e.Event.AggregateVersion)
            .ToList();

        var succeeded = new List<string>();
        var failed = new List<(string EntryId, string Reason)>();

        foreach (var entry in ordered)
        {
            var result = await ReplayAsync(entry.Id, cancellationToken);
            if (result.IsSuccess)
                succeeded.Add(entry.Id);
            else
                failed.Add((entry.Id, result.ErrorMessage ?? "unknown error"));
        }

        return Result<DeadLetterReplayBatchResult>.Success(new DeadLetterReplayBatchResult(succeeded, failed));
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refuses replay when a later event for the same aggregate stream has already been
    /// applied to the same projection - i.e. it is no longer dead-lettered (unresolved)
    /// for that projection, meaning the projector consumed it successfully and moved past
    /// the version this entry would restore.
    /// </summary>
    private async Task<Result> CheckOrderingAsync(DeadLetterEntry entry, CancellationToken cancellationToken)
    {
        var streamResult = await _eventStore.GetEventStreamAsync(entry.Event.AggregateId, cancellationToken);
        if (!streamResult.IsSuccess)
            return Result.Success();

        var laterEvents = streamResult.Data!
            .Where(e => e.AggregateVersion > entry.Event.AggregateVersion)
            .ToList();

        if (laterEvents.Count == 0)
            return Result.Success();

        var pendingForAggregate = await _deadLetterStore.GetByAggregateAsync(entry.Event.AggregateId, cancellationToken);
        var stillPendingVersions = pendingForAggregate
            .Where(e => e.ProjectionName == entry.ProjectionName && e.Id != entry.Id)
            .Select(e => e.Event.AggregateVersion)
            .ToHashSet();

        var alreadyApplied = laterEvents
            .Where(e => !stillPendingVersions.Contains(e.AggregateVersion))
            .Select(e => e.AggregateVersion)
            .ToList();

        if (alreadyApplied.Count == 0)
            return Result.Success();

        var versions = string.Join(", ", alreadyApplied);

        return Result.Failure(
            "ORDERING_VIOLATION",
            $"Cannot replay version {entry.Event.AggregateVersion} of aggregate '{entry.Event.AggregateId}' " +
            $"for projection '{entry.ProjectionName}': later version(s) {versions} were already applied.");
    }
}
