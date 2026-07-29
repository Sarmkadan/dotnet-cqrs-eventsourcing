#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.ReadModels;

using Configuration;

/// <summary>
/// Configuration options for the <see cref="ReadModelProjectionEngine"/>.
/// Bind to the <c>ReadModelProjections</c> section of your application configuration,
/// or override individual properties when calling
/// <see cref="ReadModelExtensions.AddReadModelProjections"/>.
/// </summary>
public sealed record ReadModelProjectionOptions : IValidatableOptions
{
    /// <summary>
    /// Configuration section key used when binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "ReadModelProjections";

    /// <summary>
    /// Maximum number of retry attempts when a projector throws an unhandled exception.
    /// The first execution counts as attempt zero; retries begin at attempt one.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay in milliseconds between retry attempts. Each subsequent retry
    /// doubles the delay (binary exponential back-off).
    /// Defaults to <c>100</c> ms.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; init; } = 100;

    /// <summary>
    /// When <see langword="true"/>, the engine writes a <see cref="ProjectionCheckpoint"/>
    /// every <see cref="CheckpointInterval"/> successfully processed events per projection.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableCheckpointing { get; init; } = true;

    /// <summary>
    /// Number of successfully processed events between checkpoint writes.
    /// Lower values increase durability at the cost of slightly more overhead.
    /// Defaults to <c>10</c>.
    /// </summary>
    public int CheckpointInterval { get; init; } = 10;

    /// <summary>
    /// Maximum number of projectors that may execute concurrently for a single incoming event.
    /// Setting this to <c>1</c> serialises all projection work.
    /// Defaults to <c>4</c>.
    /// </summary>
    public int MaxConcurrentProjectors { get; init; } = 4;

    /// <summary>
    /// Per-projector timeout applied to each <see cref="IReadModelProjectionRunner.RunAsync"/> call,
    /// including any read-model store I/O. Exceeding this timeout is treated as a transient failure
    /// and triggers the retry policy.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan ProjectorTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <see langword="true"/>, all in-memory checkpoints are cleared before a rebuild
    /// initiated via <see cref="ReadModelProjectionEngine.RebuildAllAsync"/> begins.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ClearCheckpointsBeforeRebuild { get; init; } = false;

    /// <summary>
    /// When <see langword="true"/>, events that exhaust all retry attempts are written to
    /// the <see cref="IDeadLetterStore"/> instead of being silently dropped.
    /// Requires an <see cref="IDeadLetterStore"/> to be registered in the DI container.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableDeadLetterStore { get; init; } = true;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any option is out of valid range.</exception>
    public void Validate()
    {
        if (MaxRetryAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetryAttempts), "MaxRetryAttempts cannot be negative.");
        }

        if (RetryBaseDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryBaseDelayMilliseconds), "RetryBaseDelayMilliseconds cannot be negative.");
        }

        if (CheckpointInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CheckpointInterval), "CheckpointInterval must be greater than zero.");
        }

        if (MaxConcurrentProjectors <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxConcurrentProjectors), "MaxConcurrentProjectors must be greater than zero.");
        }
        
        if (ProjectorTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ProjectorTimeout), "ProjectorTimeout must be greater than zero.");
        }
    }
}

/// <summary>
/// An immutable record of how far a named projection has advanced through the event stream.
/// Checkpoints allow the projection engine to detect gaps and resume after a restart.
/// </summary>
/// <param name="ProjectionName">Logical name of the projection that owns this checkpoint.</param>
/// <param name="LastProcessedEventId">Identifier of the most recent event successfully applied.</param>
/// <param name="LastProcessedVersion">Aggregate version of the most recent event successfully applied.</param>
/// <param name="WrittenAt">UTC timestamp when this checkpoint was recorded.</param>
/// <param name="TotalEventsProcessed">Cumulative count of events processed since the engine started.</param>
public sealed record ProjectionCheckpoint(
    string ProjectionName,
    string LastProcessedEventId,
    long LastProcessedVersion,
    DateTime WrittenAt,
    long TotalEventsProcessed);

/// <summary>
/// Describes the outcome of a projection rebuild operation.
/// </summary>
/// <param name="AggregateId">The aggregate whose event stream was replayed.</param>
/// <param name="EventsReplayed">Total number of events applied during the rebuild.</param>
/// <param name="FailedEventIds">Identifiers of events that could not be applied.</param>
/// <param name="CompletedAt">UTC timestamp when the rebuild finished.</param>
public sealed record ProjectionRebuildResult(
    string AggregateId,
    int EventsReplayed,
    IReadOnlyList<string> FailedEventIds,
    DateTime CompletedAt)
{
    /// <summary>Returns <see langword="true"/> when every event was applied without error.</summary>
    public bool IsFullSuccess => FailedEventIds.Count == 0;
}
