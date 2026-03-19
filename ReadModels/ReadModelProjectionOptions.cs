// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Configuration options for the <see cref="ReadModelProjectionEngine"/>.
/// Bind to the <c>ReadModelProjections</c> section of your application configuration,
/// or override individual properties when calling
/// <see cref="ReadModelExtensions.AddReadModelProjections"/>.
/// </summary>
public sealed class ReadModelProjectionOptions
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
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds between retry attempts. Each subsequent retry
    /// doubles the delay (binary exponential back-off).
    /// Defaults to <c>100</c> ms.
    /// </summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// When <see langword="true"/>, the engine writes a <see cref="ProjectionCheckpoint"/>
    /// every <see cref="CheckpointInterval"/> successfully processed events per projection.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableCheckpointing { get; set; } = true;

    /// <summary>
    /// Number of successfully processed events between checkpoint writes.
    /// Lower values increase durability at the cost of slightly more overhead.
    /// Defaults to <c>10</c>.
    /// </summary>
    public int CheckpointInterval { get; set; } = 10;

    /// <summary>
    /// Maximum number of projectors that may execute concurrently for a single incoming event.
    /// Setting this to <c>1</c> serialises all projection work.
    /// Defaults to <c>4</c>.
    /// </summary>
    public int MaxConcurrentProjectors { get; set; } = 4;

    /// <summary>
    /// Per-projector timeout applied to each <see cref="IReadModelProjectionRunner.RunAsync"/> call,
    /// including any read-model store I/O. Exceeding this timeout is treated as a transient failure
    /// and triggers the retry policy.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan ProjectorTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <see langword="true"/>, all in-memory checkpoints are cleared before a rebuild
    /// initiated via <see cref="ReadModelProjectionEngine.RebuildAllAsync"/> begins.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ClearCheckpointsBeforeRebuild { get; set; } = false;
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
