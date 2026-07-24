using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Source of events for a <see cref="ProjectionEngine"/> projection.
/// Implementations pull the next event after a given checkpoint.
/// </summary>
public interface IProjectionEventSource
{
    /// <summary>
    /// Returns the next event for <paramref name="projectionName"/> after
    /// <paramref name="checkpoint"/>, or <c>null</c> when the projection is
    /// caught up. The engine polls again after a short delay when <c>null</c>
    /// is returned.
    /// </summary>
    /// <param name="projectionName">Name of the projection asking for events.</param>
    /// <param name="checkpoint">Last processed event identifier, or <c>null</c> when starting from the beginning.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    Task<string?> GetNextEventAsync(string projectionName, string? checkpoint, CancellationToken cancellationToken);
}

/// <summary>
/// Executes a projection by continuously pulling events from an
/// <see cref="IProjectionEventSource"/> and invoking a user‑provided processing
/// delegate. The engine tracks a checkpoint per projection name so that
/// processing can resume after a restart. The engine follows at‑least‑once
/// semantics: an event may be delivered more than once, therefore the
/// processing delegate must be idempotent or implement its own atomic
/// checkpoint handling.
/// </summary>
public sealed class ProjectionEngine
{
    private readonly ILogger<ProjectionEngine> _logger;
    private readonly IProjectionEventSource _eventSource;
    private readonly ConcurrentDictionary<string, ProjectionState> _projections = new();

    /// <summary>
    /// Creates a new <see cref="ProjectionEngine"/>.
    /// </summary>
    /// <param name="logger">Logger used for diagnostics.</param>
    /// <param name="eventSource">Source the engine pulls events from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="eventSource"/> is <c>null</c>.</exception>
    public ProjectionEngine(ILogger<ProjectionEngine> logger, IProjectionEventSource eventSource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
    }

    /// <summary>
    /// Runs a projection using a simple <c>Func&lt;string, Task&gt;</c> delegate.
    /// This overload preserves the original contract and treats the delegate as
    /// always succeeding; the checkpoint is advanced after the delegate completes.
    /// </summary>
    /// <param name="projectionName">Unique name of the projection.</param>
    /// <param name="processEvent">Delegate that processes a single event.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the engine stops (usually never).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectionName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processEvent"/> is <c>null</c>.</exception>
    public Task RunAsync(string projectionName, Func<string, Task> processEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processEvent);
        return RunAsync(projectionName, async @event =>
        {
            await processEvent(@event).ConfigureAwait(false);
            // The original contract always advances the checkpoint after the delegate finishes.
            return true;
        }, cancellationToken);
    }

    /// <summary>
    /// Runs a projection using a delegate that returns a <c>bool</c> indicating whether
    /// the checkpoint should be advanced. This enables callers to perform the
    /// projection write and checkpoint persistence atomically (e.g., within a
    /// transaction). The checkpoint is only updated when the delegate returns <c>true</c>.
    /// </summary>
    /// <param name="projectionName">Unique name of the projection.</param>
    /// <param name="processEventAndCheckpoint">
    /// Delegate that processes an event and returns <c>true</c> if the checkpoint may be
    /// safely advanced; otherwise <c>false</c>.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the engine stops (usually never).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="projectionName"/> is <c>null</c> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="processEventAndCheckpoint"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the projection is already running.</exception>
    public async Task RunAsync(string projectionName, Func<string, Task<bool>> processEventAndCheckpoint, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectionName);
        ArgumentNullException.ThrowIfNull(processEventAndCheckpoint);

        var projectionState = _projections.GetOrAdd(projectionName, static name => new ProjectionState(name));

        if (Interlocked.CompareExchange(ref projectionState.Running, 1, 0) != 0)
            throw new InvalidOperationException($"Projection '{projectionName}' is already running.");

        try
        {
            await RunProjectionAsync(projectionState, processEventAndCheckpoint, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref projectionState.Running, 0);
        }
    }

    private async Task RunProjectionAsync(ProjectionState projectionState, Func<string, Task<bool>> processEventAndCheckpoint, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var @event = await _eventSource
                    .GetNextEventAsync(projectionState.Name, projectionState.Checkpoint, cancellationToken)
                    .ConfigureAwait(false);
                if (@event is null)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var shouldAdvance = await processEventAndCheckpoint(@event).ConfigureAwait(false);
                if (shouldAdvance)
                {
                    projectionState.Checkpoint = @event;
                    projectionState.ConsecutiveFailures = 0;
                }
                else
                {
                    // The delegate decided not to advance the checkpoint (e.g., transaction failed).
                    // We keep the current checkpoint so the event will be retried.
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event in projection {ProjectionName}", projectionState.Name);
                projectionState.ConsecutiveFailures++;

                if (projectionState.ConsecutiveFailures >= 5)
                {
                    _logger.LogInformation("Circuit breaker triggered for projection {ProjectionName}. Pausing projection.", projectionState.Name);
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    projectionState.ConsecutiveFailures = 0;
                }
            }
        }
    }

    private sealed class ProjectionState
    {
        /// <summary>
        /// Name of the projection (used for logging and checkpoint tracking).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The last successfully processed event identifier. <c>null</c> means no events processed yet.
        /// </summary>
        public string? Checkpoint { get; set; }

        /// <summary>
        /// Number of consecutive failures; used for circuit‑breaker logic.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// 1 while a RunAsync loop owns this projection; guards against concurrent runs.
        /// </summary>
        public int Running;

        public ProjectionState(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
