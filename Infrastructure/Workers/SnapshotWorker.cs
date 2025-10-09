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
/// Runs on a schedule (default every 5 minutes) and only processes unchanged aggregates.
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
            var account = Account.LoadFromHistory(aggregateId, events.Cast<dynamic>().ToList());

            // Create and persist snapshot
            var snapshot = new AggregateSnapshot
            {
                AggregateId = aggregateId,
                AggregateType = account.GetType().Name,
                State = account,
                EventVersion = events.Count,
                CreatedAt = DateTime.UtcNow
            };

            await _snapshotService.SaveSnapshotAsync(snapshot, cancellationToken);

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
    /// Currently a placeholder for integration with aggregate discovery.
    /// In production, this would query tracked aggregates from a database or registry.
    /// </summary>
    private async Task ProcessSnapshotsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing snapshots - interval cycle");

        // In a real implementation, this would:
        // 1. Query list of aggregate IDs from a registry/database
        // 2. For each aggregate:
        //    - Check if events since last snapshot exceed threshold
        //    - If yes, create new snapshot
        //    - Track which aggregates were processed

        // For now, this is a placeholder that demonstrates the structure
        await Task.CompletedTask;
    }
}
