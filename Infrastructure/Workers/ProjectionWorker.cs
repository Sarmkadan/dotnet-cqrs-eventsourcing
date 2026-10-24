#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Health;

namespace DotNetCqrsEventSourcing.Infrastructure.Workers;

/// <summary>
/// Background worker that maintains read model projections from the event stream.
/// Subscribes to domain events and updates denormalized read models for fast querying.
/// Implements event-driven architecture: projections stay eventually consistent with event stream.
/// Handles projection rebuild, catch-up after outages, and backpressure on slow projections.
/// </summary>
public interface IProjectionWorker : IHostedService, IHealthContributor
{
    /// <summary>
    /// Manually triggers projection rebuild from event stream.
    /// Useful after detecting projection corruption or implementing new projection logic.
    /// </summary>
    Task RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses projection processing (e.g., during maintenance).
    /// Useful to stop processing while fixing projection bugs.
    /// </summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes projection processing after being paused.
    /// </summary>
    Task ResumeAsync(CancellationToken cancellationToken = default);
}

public class ProjectionWorker : BackgroundService, IProjectionWorker
{
    private readonly IEventStore _eventStore;
    private readonly IProjectionService _projectionService;
    private readonly ILogger<ProjectionWorker> _logger;
    private bool _isPaused = false;
    private readonly object _pauseLock = new();

    private const int PausedPollingDelayMs = 1000;
    private const int NormalPollingDelayMs = 1000;
    private const int ErrorBackoffDelayMs = 5000;
    private const string HealthComponentName = "projection-worker";
    private static readonly TimeSpan HeartbeatStaleAfter = TimeSpan.FromSeconds(30);

    private readonly object _healthLock = new();
    private bool _isRunning;
    private DateTime? _lastHeartbeatUtc;
    private int _consecutiveErrors;

    public ProjectionWorker(
        IEventStore eventStore,
        IProjectionService projectionService,
        ILogger<ProjectionWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(projectionService);
        ArgumentNullException.ThrowIfNull(logger);
        _eventStore = eventStore;
        _projectionService = projectionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Projection worker started");
        SetRunning(true);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    RecordHeartbeat();

                    // Check if paused
                    lock (_pauseLock)
                    {
                        if (_isPaused)
                        {
                            _logger.LogDebug("Projection worker is paused, waiting for resume");
                            await Task.Delay(PausedPollingDelayMs, stoppingToken);
                            continue;
                        }
                    }

                    // Process projections
                    await ProcessProjectionsAsync(stoppingToken);
                    RecordSuccessfulCycle();

                    // Yield to allow other work
                    await Task.Delay(NormalPollingDelayMs, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing projections");
                    RecordFailedCycle();
                    await Task.Delay(ErrorBackoffDelayMs, stoppingToken); // Back off on error
                }
            }
        }
        finally
        {
            SetRunning(false);
        }

        _logger.LogInformation("Projection worker stopped");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Reports one <c>projection-worker</c> component. The worker is unhealthy when its loop is not
    /// running or its heartbeat is older than 30 seconds, and degraded when paused or failing.
    /// </remarks>
    public Task<IReadOnlyList<ComponentHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        bool isRunning;
        DateTime? lastHeartbeat;
        int consecutiveErrors;

        lock (_healthLock)
        {
            isRunning = _isRunning;
            lastHeartbeat = _lastHeartbeatUtc;
            consecutiveErrors = _consecutiveErrors;
        }

        var component = WorkerLiveness.Describe(
            HealthComponentName,
            isRunning,
            IsPaused,
            lastHeartbeat,
            HeartbeatStaleAfter,
            consecutiveErrors,
            DateTime.UtcNow);

        return Task.FromResult<IReadOnlyList<ComponentHealth>>([component]);
    }

    private void SetRunning(bool isRunning)
    {
        lock (_healthLock)
        {
            _isRunning = isRunning;
        }
    }

    private void RecordHeartbeat()
    {
        lock (_healthLock)
        {
            _lastHeartbeatUtc = DateTime.UtcNow;
        }
    }

    private void RecordSuccessfulCycle()
    {
        lock (_healthLock)
        {
            _consecutiveErrors = 0;
        }
    }

    private void RecordFailedCycle()
    {
        lock (_healthLock)
        {
            _consecutiveErrors++;
        }
    }

    public async Task RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(aggregateId);

        _logger.LogWarning("Rebuilding projection for aggregate {AggregateId}", aggregateId);

        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            await _projectionService.RebuildProjectionAsync(aggregateId, events, cancellationToken);

            _logger.LogInformation(
                "Projection rebuilt for aggregate {AggregateId} from {EventCount} events",
                aggregateId,
                events.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding projection for aggregate {AggregateId}", aggregateId);
        }
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        lock (_pauseLock)
        {
            _isPaused = true;
            _logger.LogInformation("Projection worker paused");
        }

        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        lock (_pauseLock)
        {
            _isPaused = false;
            _logger.LogInformation("Projection worker resumed");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Main projection processing loop.
    /// In a real implementation, this would:
    /// 1. Fetch events since last processed position
    /// 2. Apply events to projections
    /// 3. Handle failures and retry logic
    /// 4. Track processing state for recovery after crashes
    /// </summary>
    private async Task ProcessProjectionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing projections");

        // Placeholder for actual projection processing
        // In production, this would:
        // - Query list of aggregates
        // - For each aggregate, check if events since last projection update
        // - Apply events to read models
        // - Handle backpressure if projections can't keep up

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current pause state (useful for health checks).
    /// </summary>
    public bool IsPaused
    {
        get
        {
            lock (_pauseLock)
            {
                return _isPaused;
            }
        }
    }
}
