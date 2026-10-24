#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Health;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Options;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Orchestrates eventually consistent read-model projections by subscribing to the
/// application event bus and routing each domain event to every registered
/// <see cref="IReadModelProjectionRunner"/>.
/// <para>
/// Features: configurable retry with exponential back-off, per-projection checkpointing,
/// bounded concurrency, on-demand aggregate replay for full or partial rebuilds, and
/// health reporting (projection lag and failure streaks) through <see cref="IHealthContributor"/>.
/// </para>
/// </summary>
public sealed class ReadModelProjectionEngine : IDisposable, IHealthContributor
{
    private const string EngineComponentName = "read-model-engine";
    private const string ProjectionComponentPrefix = "projection:";
    private const long LagDegradedThreshold = 1000;

    private readonly IEventBus _eventBus;
    private readonly IEventStore _eventStore;
    private readonly IReadOnlyList<IReadModelProjectionRunner> _runners;
    private readonly ReadModelProjectionOptions _options;
    private readonly ILogger<ReadModelProjectionEngine> _logger;
    private readonly IDeadLetterStore? _deadLetterStore;

    // Stored to allow clean unsubscription on Dispose.
    private readonly Func<DomainEvent, Task> _eventHandler;

    private readonly ConcurrentDictionary<string, ProjectionCheckpoint> _checkpoints = new();
    private readonly ConcurrentDictionary<string, long> _processingCounters = new();
    private readonly ConcurrentDictionary<string, long> _headPositions = new();
    private readonly ConcurrentDictionary<string, int> _failureStreaks = new();
    private long _totalEventsRouted;
    private bool _disposed;

    /// <summary>
    /// Initializes the engine and registers a base-type subscription on
    /// <paramref name="eventBus"/> so that every published domain event is received.
    /// </summary>
    public ReadModelProjectionEngine(
        IEventBus eventBus,
        IEventStore eventStore,
        IEnumerable<IReadModelProjectionRunner> runners,
        IOptions<ReadModelProjectionOptions> options,
        ILogger<ReadModelProjectionEngine> logger,
        IDeadLetterStore? deadLetterStore = null)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(runners);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _eventBus = eventBus;
        _eventStore = eventStore;
        _runners = runners.ToList();
        _options = options?.Value ?? new ReadModelProjectionOptions();
        _logger = logger;
        _deadLetterStore = deadLetterStore;

        _eventHandler = @event => HandleEventAsync(@event);
        _eventBus.Subscribe<DomainEvent>(_eventHandler);

        _logger.LogInformation(
            "ReadModelProjectionEngine started with {RunnerCount} registered runner(s).",
            _runners.Count);
    }

    // -------------------------------------------------------------------------
    // Public surface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Read-only view of all checkpoints written since the engine started.
    /// Keyed by <see cref="ProjectionCheckpoint.ProjectionName"/>.
    /// </summary>
    public IReadOnlyDictionary<string, ProjectionCheckpoint> Checkpoints => _checkpoints;

    /// <summary>Total events received from the bus since the engine was created.</summary>
    public long TotalEventsRouted => Interlocked.Read(ref _totalEventsRouted);

    /// <summary>
    /// Rebuilds all projections for a single aggregate by replaying its complete event stream
    /// from the <see cref="IEventStore"/>.
    /// </summary>
    /// <param name="aggregateId">Identifier of the aggregate to rebuild.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ProjectionRebuildResult"/> describing which events were applied and
    /// which (if any) failed, wrapped in a <see cref="Result{T}"/>.
    /// </returns>
    public async Task<Result<ProjectionRebuildResult>> RebuildAsync(
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        _logger.LogInformation("Starting projection rebuild for aggregate {AggregateId}.", aggregateId);

        var eventsResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken);
        if (!eventsResult.IsSuccess)
        {
            return Result<ProjectionRebuildResult>.Failure(eventsResult.ErrorCode!, eventsResult.ErrorMessage!);
        }

        var events = eventsResult.Data!;
        var failedIds = new List<string>();

        foreach (var @event in events)
        {
            var routeResult = await RouteAsync(@event, cancellationToken);
            if (!routeResult.IsSuccess)
            {
                failedIds.Add(@event.EventId);
            }
        }

        var summary = new ProjectionRebuildResult(aggregateId, events.Count, failedIds, DateTime.UtcNow);

        _logger.LogInformation(
            "Projection rebuild completed for {AggregateId}: {Total} event(s) replayed, {Failures} failure(s).",
            aggregateId, events.Count, failedIds.Count);

        return Result<ProjectionRebuildResult>.Success(summary);
    }

    /// <summary>
    /// Rebuilds all projections across the supplied set of aggregate identifiers.
    /// Stops and propagates the first store-level failure; partial projector failures
    /// are recorded in each <see cref="ProjectionRebuildResult"/> instead.
    /// </summary>
    /// <param name="aggregateIds">Ordered sequence of aggregate identifiers to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<Result<IReadOnlyList<ProjectionRebuildResult>>> RebuildAllAsync(
        IEnumerable<string> aggregateIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateIds);

        if (_options.ClearCheckpointsBeforeRebuild)
        {
            _checkpoints.Clear();
        }

        var results = new List<ProjectionRebuildResult>();

        foreach (var id in aggregateIds)
        {
            var result = await RebuildAsync(id, cancellationToken);
            if (!result.IsSuccess)
            {
                return Result<IReadOnlyList<ProjectionRebuildResult>>.Failure(
                    result.ErrorCode!, result.ErrorMessage!);
            }

            results.Add(result.Data!);
        }

        return Result<IReadOnlyList<ProjectionRebuildResult>>.Success(results);
    }

    /// <summary>Returns the checkpoint for <paramref name="projectionName"/>, or <see langword="null"/> if none exists.</summary>
    public ProjectionCheckpoint? GetCheckpoint(string projectionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionName);
        return _checkpoints.GetValueOrDefault(projectionName);
    }

    /// <summary>
    /// Reports the engine and one component per registered projection. For each projection the
    /// metrics are <c>head</c> (events routed to it), <c>checkpoint</c> (events it has applied),
    /// <c>persisted_checkpoint</c> (events covered by the last written checkpoint), <c>lag</c>
    /// (<c>head</c> minus <c>checkpoint</c>) and <c>failure_streak</c> (consecutive permanent failures).
    /// A projection is degraded when its streak is non-zero or its lag exceeds 1000 events.
    /// The engine is unhealthy once it has been disposed and no longer receives events.
    /// </summary>
    /// <param name="cancellationToken">Not used; the probe is synchronous and in-memory.</param>
    /// <returns>The engine component followed by one component per registered projection.</returns>
    public Task<IReadOnlyList<ComponentHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var engineComponent = new ComponentHealth(
            EngineComponentName,
            _disposed ? HealthState.Unhealthy : HealthState.Healthy,
            _disposed ? "Engine is disposed and no longer receives events." : "Engine is receiving events.",
            new Dictionary<string, long>
            {
                ["events_routed"] = TotalEventsRouted,
                ["projections"] = _runners.Count
            });

        var components = new List<ComponentHealth> { engineComponent };

        foreach (var projectionName in _runners.Select(runner => runner.ProjectionName).Distinct(StringComparer.Ordinal))
        {
            components.Add(DescribeProjection(projectionName));
        }

        return Task.FromResult<IReadOnlyList<ComponentHealth>>(components);
    }

    // -------------------------------------------------------------------------
    // Event handling
    // -------------------------------------------------------------------------

    private async Task HandleEventAsync(DomainEvent @event)
    {
        Interlocked.Increment(ref _totalEventsRouted);

        var result = await RouteAsync(@event, CancellationToken.None);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "Projection routing failed for event {EventId} ({EventType}): {Error}",
                @event.EventId, @event.GetEventType(), result.ErrorMessage);
        }
    }

    private async Task<Result> RouteAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        var capable = _runners.Where(r => r.CanHandle(@event)).ToList();
        if (capable.Count == 0)
        {
            return Result.Success();
        }

        foreach (var runner in capable)
        {
            _headPositions.AddOrUpdate(runner.ProjectionName, 1, static (_, position) => position + 1);
        }

        using var semaphore = new SemaphoreSlim(_options.MaxConcurrentProjectors, _options.MaxConcurrentProjectors);
        var errors = new ConcurrentBag<string>();

        await Task.WhenAll(capable.Select(async runner =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await ApplyWithRetryAsync(runner, @event, cancellationToken);
                if (!result.IsSuccess)
                {
                    errors.Add($"[{runner.ProjectionName}] {result.ErrorMessage}");

                    // A cancelled shutdown is not a projector failure, so it does not extend the streak.
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _failureStreaks.AddOrUpdate(runner.ProjectionName, 1, static (_, streak) => streak + 1);
                    }
                }
                else
                {
                    AdvanceCheckpoint(runner.ProjectionName, @event);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }));

        return errors.IsEmpty
            ? Result.Success()
            : Result.Failure("PROJECTION_ROUTING_ERROR", string.Join("; ", errors));
    }

    private async Task<Result> ApplyWithRetryAsync(
        IReadModelProjectionRunner runner,
        DomainEvent @event,
        CancellationToken cancellationToken)
    {
        var delay = _options.RetryBaseDelayMilliseconds;

        for (var attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.ProjectorTimeout);
                return await runner.RunAsync(@event, cts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Result.Failure("PROJECTION_CANCELLED", "Projection processing was cancelled.");
            }
            catch (Exception ex)
            {
                if (attempt == _options.MaxRetryAttempts)
                {
                    _logger.LogError(ex,
                        "Projector '{Name}' failed permanently after {Attempts} attempt(s) for event {EventId}.",
                        runner.ProjectionName, attempt + 1, @event.EventId);

                    if (_options.EnableDeadLetterStore && _deadLetterStore is not null)
                    {
                        var entry = new DeadLetterEntry
                        {
                            Event = @event,
                            ProjectionName = runner.ProjectionName,
                            ErrorMessage = ex.Message,
                            AttemptCount = attempt + 1
                        };

                        await _deadLetterStore.WriteAsync(entry, cancellationToken);
                    }

                    return Result.Failure("PROJECTOR_FAILED", ex.Message);
                }

                _logger.LogWarning(ex,
                    "Projector '{Name}' attempt {Attempt} failed for event {EventId}; retrying in {Delay} ms.",
                    runner.ProjectionName, attempt + 1, @event.EventId, delay);

                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }

        return Result.Failure("PROJECTOR_FAILED", "Retry loop exited without result.");
    }

    private void AdvanceCheckpoint(string projectionName, DomainEvent @event)
    {
        // A successful apply ends any failure streak for this projection.
        _failureStreaks[projectionName] = 0;

        var total = _processingCounters.AddOrUpdate(projectionName, 1, (_, c) => c + 1);

        if (!_options.EnableCheckpointing || total % _options.CheckpointInterval != 0)
        {
            return;
        }

        _checkpoints[projectionName] = new ProjectionCheckpoint(
            projectionName,
            @event.EventId,
            @event.AggregateVersion,
            DateTime.UtcNow,
            total);
    }

    private ComponentHealth DescribeProjection(string projectionName)
    {
        var head = _headPositions.GetValueOrDefault(projectionName);
        var checkpoint = _processingCounters.GetValueOrDefault(projectionName);
        var persistedCheckpoint = _checkpoints.TryGetValue(projectionName, out var stored) ? stored.TotalEventsProcessed : 0;
        var failureStreak = _failureStreaks.GetValueOrDefault(projectionName);
        var lag = head - checkpoint;

        var (state, description) = (failureStreak, lag) switch
        {
            ( > 0, _) => (HealthState.Degraded, $"{failureStreak} consecutive failure(s); events are being retried or dead-lettered."),
            (_, > LagDegradedThreshold) => (HealthState.Degraded, $"Lag of {lag} event(s) exceeds {LagDegradedThreshold}."),
            (_, > 0) => (HealthState.Healthy, $"Lag of {lag} event(s); may include dead-lettered events awaiting replay."),
            _ => (HealthState.Healthy, "Caught up with the event stream.")
        };

        return new ComponentHealth(
            ProjectionComponentPrefix + projectionName,
            state,
            description,
            new Dictionary<string, long>
            {
                ["head"] = head,
                ["checkpoint"] = checkpoint,
                ["persisted_checkpoint"] = persistedCheckpoint,
                ["lag"] = lag,
                ["failure_streak"] = failureStreak
            });
    }

    // -------------------------------------------------------------------------
    // IDisposable
    // -------------------------------------------------------------------------

    /// <summary>Unsubscribes from the event bus and releases managed resources.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _eventBus.Unsubscribe<DomainEvent>(_eventHandler);
        _disposed = true;

        _logger.LogInformation("ReadModelProjectionEngine disposed and unsubscribed from event bus.");
    }
}
