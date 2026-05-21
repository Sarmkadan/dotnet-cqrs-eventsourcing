#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Orchestrates eventually consistent read-model projections by subscribing to the
/// application event bus and routing each domain event to every registered
/// <see cref="IReadModelProjectionRunner"/>.
/// <para>
/// Features: configurable retry with exponential back-off, per-projection checkpointing,
/// bounded concurrency, and on-demand aggregate replay for full or partial rebuilds.
/// </para>
/// </summary>
public sealed class ReadModelProjectionEngine : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IEventStore _eventStore;
    private readonly IReadOnlyList<IReadModelProjectionRunner> _runners;
    private readonly ReadModelProjectionOptions _options;
    private readonly ILogger<ReadModelProjectionEngine> _logger;

    // Stored to allow clean unsubscription on Dispose.
    private readonly Func<DomainEvent, Task> _eventHandler;

    private readonly ConcurrentDictionary<string, ProjectionCheckpoint> _checkpoints = new();
    private readonly ConcurrentDictionary<string, long> _processingCounters = new();
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
        ILogger<ReadModelProjectionEngine> logger)
    {
        _eventBus = GuardClauses.NotNull(eventBus, nameof(eventBus));
        _eventStore = GuardClauses.NotNull(eventStore, nameof(eventStore));
        _runners = GuardClauses.NotNull(runners, nameof(runners)).ToList();
        _options = options?.Value ?? new ReadModelProjectionOptions();
        _logger = GuardClauses.NotNull(logger, nameof(logger));

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
        GuardClauses.NotNullOrEmpty(aggregateId, nameof(aggregateId));

        _logger.LogInformation("Starting projection rebuild for aggregate {AggregateId}.", aggregateId);

        var eventsResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken).ConfigureAwait(false);
        if (!eventsResult.IsSuccess)
            return Result<ProjectionRebuildResult>.Failure(eventsResult.ErrorCode!, eventsResult.ErrorMessage!);

        var events = eventsResult.Data!;
        var failedIds = new List<string>();

        foreach (var @event in events)
        {
            var routeResult = await RouteAsync(@event, cancellationToken).ConfigureAwait(false);
            if (!routeResult.IsSuccess)
                failedIds.Add(@event.EventId);
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
        GuardClauses.NotNull(aggregateIds, nameof(aggregateIds));

        if (_options.ClearCheckpointsBeforeRebuild)
            _checkpoints.Clear();

        var results = new List<ProjectionRebuildResult>();

        foreach (var id in aggregateIds)
        {
            var result = await RebuildAsync(id, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                return Result<IReadOnlyList<ProjectionRebuildResult>>.Failure(
                    result.ErrorCode!, result.ErrorMessage!);

            results.Add(result.Data!);
        }

        return Result<IReadOnlyList<ProjectionRebuildResult>>.Success(results);
    }

    /// <summary>Returns the checkpoint for <paramref name="projectionName"/>, or <see langword="null"/> if none exists.</summary>
    public ProjectionCheckpoint? GetCheckpoint(string projectionName) =>
        _checkpoints.GetValueOrDefault(projectionName);

    // -------------------------------------------------------------------------
    // Event handling
    // -------------------------------------------------------------------------

    private async Task HandleEventAsync(DomainEvent @event)
    {
        Interlocked.Increment(ref _totalEventsRouted);

        var result = await RouteAsync(@event, CancellationToken.None).ConfigureAwait(false);

        if (!result.IsSuccess)
            _logger.LogError(
                "Projection routing failed for event {EventId} ({EventType}): {Error}",
                @event.EventId, @event.GetEventType(), result.ErrorMessage);
    }

    private async Task<Result> RouteAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        var capable = _runners.Where(r => r.CanHandle(@event)).ToList();
        if (capable.Count == 0)
            return Result.Success();

        using var semaphore = new SemaphoreSlim(_options.MaxConcurrentProjectors, _options.MaxConcurrentProjectors);
        var errors = new ConcurrentBag<string>();

        await Task.WhenAll(capable.Select(async runner =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await ApplyWithRetryAsync(runner, @event, cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                    errors.Add($"[{runner.ProjectionName}] {result.ErrorMessage}");
                else
                    AdvanceCheckpoint(runner.ProjectionName, @event);
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
                return await runner.RunAsync(@event, cts.Token).ConfigureAwait(false);
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
                    return Result.Failure("PROJECTOR_FAILED", ex.Message);
                }

                _logger.LogWarning(ex,
                    "Projector '{Name}' attempt {Attempt} failed for event {EventId}; retrying in {Delay} ms.",
                    runner.ProjectionName, attempt + 1, @event.EventId, delay);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay *= 2;
            }
        }

        return Result.Failure("PROJECTOR_FAILED", "Retry loop exited without result.");
    }

    private void AdvanceCheckpoint(string projectionName, DomainEvent @event)
    {
        if (!_options.EnableCheckpointing)
            return;

        var total = _processingCounters.AddOrUpdate(projectionName, 1, (_, c) => c + 1);

        if (total % _options.CheckpointInterval != 0)
            return;

        _checkpoints[projectionName] = new ProjectionCheckpoint(
            projectionName,
            @event.EventId,
            @event.AggregateVersion,
            DateTime.UtcNow,
            total);
    }

    // -------------------------------------------------------------------------
    // IDisposable
    // -------------------------------------------------------------------------

    /// <summary>Unsubscribes from the event bus and releases managed resources.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _eventBus.Unsubscribe<DomainEvent>(_eventHandler);
        _disposed = true;

        _logger.LogInformation("ReadModelProjectionEngine disposed and unsubscribed from event bus.");
    }
}
