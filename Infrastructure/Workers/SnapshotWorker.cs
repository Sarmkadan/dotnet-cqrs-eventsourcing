#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;

namespace DotNetCqrsEventSourcing.Infrastructure.Workers;

/// <summary>
/// Background worker that periodically creates snapshots of aggregate states.
/// Snapshots are used to avoid replaying all events when reconstructing an aggregate.
/// Without snapshots, reading an old aggregate requires replaying 1000s of events.
/// With snapshots, only recent events need replay (e.g., last 100 events since snapshot).
/// Runs on a schedule (default every 5 minutes) and only processes aggregates that exceed the event threshold.
/// </summary>
public interface ISnapshotWorker : IHostedService
{
    /// <summary>
    /// Manually triggers snapshot creation for a specific aggregate.
    /// </summary>
    Task CreateSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
}

public class SnapshotWorker : BackgroundService, ISnapshotWorker
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<SnapshotWorker> _logger;
    private readonly TimeSpan _snapshotInterval;
    private readonly int _eventsThresholdForSnapshot;

    public SnapshotWorker(
        IEventStore eventStore,
        ISnapshotService snapshotService,
        ILogger<SnapshotWorker> logger,
        TimeSpan? snapshotInterval = null,
        int eventsThresholdForSnapshot = 100)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _snapshotInterval = snapshotInterval ?? TimeSpan.FromMinutes(5);
        _eventsThresholdForSnapshot = eventsThresholdForSnapshot;
    }

    /// <summary>
    /// Runs the snapshot worker on a periodic schedule.
    /// Finds aggregates with many events since last snapshot and creates new snapshots.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Snapshot worker started with interval: {Interval}s", _snapshotInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSnapshotsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in snapshot worker processing");
            }

            // Wait for the next interval before processing again
            try
            {
                await Task.Delay(_snapshotInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Snapshot worker stopped");
    }

    public async Task CreateSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(aggregateId);

        _logger.LogInformation("Creating snapshot for aggregate {AggregateId}", aggregateId);

        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);

            if (events.Count == 0)
            {
                _logger.LogWarning("No events found for aggregate {AggregateId}, skipping snapshot", aggregateId);
                return;
            }

            // Rebuild aggregate state from events
            var account = new Account(aggregateId);
            account.ReplayEvents(events);

            // Create and persist snapshot
            var result = await _snapshotService.CreateSnapshotAsync(account, aggregateId, events.Count, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to save snapshot for aggregate {AggregateId}: {Error}", aggregateId, result.Error);
                return;
            }

            _logger.LogInformation(
                "Snapshot created for aggregate {AggregateId} at event version {Version}",
                aggregateId,
                events.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating snapshot for aggregate {AggregateId}", aggregateId);
        }
    }

    /// <summary>
    /// Processes all aggregates that need snapshots.
    /// Finds aggregates where the number of events since the last snapshot exceeds the threshold,
    /// then creates new snapshots for those aggregates.
    /// </summary>
    private async Task ProcessSnapshotsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing snapshots - interval cycle");

        try
        {
            // Get all aggregate IDs that have events
            // In a production system, this would query a registry or aggregate repository
            // For this implementation, we'll query the event store for distinct aggregate IDs
            var result = await _eventStore.GetEventsByTypeAsync("AccountCreatedEvent", cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Failed to retrieve aggregate IDs for snapshot processing: {Error}", result.Error);
                return;
            }

            // Get distinct aggregate IDs from events
            var aggregateIds = result.Data
                .Select(e => e.AggregateId)
                .Distinct()
                .ToList();

            if (aggregateIds.Count == 0)
            {
                _logger.LogDebug("No aggregates found for snapshot processing");
                return;
            }

            _logger.LogInformation("Processing {Count} aggregates for snapshots", aggregateIds.Count);

            // Process each aggregate
            var snapshotsCreated = 0;
            foreach (var aggregateId in aggregateIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Check if aggregate already has a snapshot
                    var hasSnapshotResult = await _snapshotService.HasSnapshotAsync(aggregateId, cancellationToken);
                    if (!hasSnapshotResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Failed to check for existing snapshot for aggregate {AggregateId}: {Error}",
                            aggregateId,
                            hasSnapshotResult.Error
                        );
                        continue;
                    }

                    // Get current aggregate version
                    var versionResult = await _eventStore.GetAggregateVersionAsync(aggregateId, cancellationToken);
                    if (!versionResult.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Failed to get version for aggregate {AggregateId}: {Error}",
                            aggregateId,
                            versionResult.Error
                        );
                        continue;
                    }

                    var currentVersion = versionResult.Data;
                    long lastSnapshotVersion = 0;

                    if (hasSnapshotResult.Data)
                    {
                        // Get latest snapshot to determine events since snapshot
                        var snapshotResult = await _snapshotService.GetLatestSnapshotAsync(aggregateId, cancellationToken);
                        if (snapshotResult.IsSuccess && snapshotResult.Data != null)
                        {
                            lastSnapshotVersion = snapshotResult.Data.Version;
                        }
                    }

                    // Calculate events since last snapshot
                    var eventsSinceSnapshot = currentVersion - lastSnapshotVersion;

                    _logger.LogDebug(
                        "Aggregate {AggregateId}: Current version={CurrentVersion}, Last snapshot={LastSnapshotVersion}, Events since snapshot={EventsSinceSnapshot}",
                        aggregateId,
                        currentVersion,
                        lastSnapshotVersion,
                        eventsSinceSnapshot
                    );

                    // Create snapshot if threshold exceeded
                    if (eventsSinceSnapshot >= _eventsThresholdForSnapshot)
                    {
                        _logger.LogInformation(
                            "Creating snapshot for aggregate {AggregateId} - {EventsSinceSnapshot} events since last snapshot (threshold: {Threshold})",
                            aggregateId,
                            eventsSinceSnapshot,
                            _eventsThresholdForSnapshot
                        );

                        await CreateSnapshotAsync(aggregateId, cancellationToken);
                        snapshotsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing aggregate {AggregateId} for snapshots", aggregateId);
                }
            }

            _logger.LogInformation("Snapshot processing complete. Created {Count} new snapshots", snapshotsCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSnapshotsAsync");
        }
    }
}
