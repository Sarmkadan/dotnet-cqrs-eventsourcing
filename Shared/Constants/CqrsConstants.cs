#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared.Constants;

/// <summary>
/// Contains application-wide constants for the CQRS framework.
/// </summary>
public static class CqrsConstants
{
    // Event store configuration
    /// <summary>
    /// Number of events after which a new aggregate snapshot is taken.
    /// </summary>
    public const int DefaultSnapshotFrequency = 10;

    /// <summary>
    /// Maximum number of events read or written in a single batch.
    /// </summary>
    public const int MaxEventsPerBatch = 1000;

    /// <summary>
    /// Number of events processed per batch during event replay.
    /// </summary>
    public const int EventReplayBatchSize = 100;

    // Timeout configurations

    /// <summary>
    /// Default timeout, in seconds, for command execution.
    /// </summary>
    public const int DefaultCommandTimeoutSeconds = 30;

    /// <summary>
    /// Default timeout, in seconds, for query execution.
    /// </summary>
    public const int DefaultQueryTimeoutSeconds = 60;

    /// <summary>
    /// Default timeout, in seconds, for publishing an event to the event bus.
    /// </summary>
    public const int DefaultEventPublishTimeoutSeconds = 15;

    // Version and metadata

    /// <summary>
    /// Version of the CQRS framework.
    /// </summary>
    public const string FrameworkVersion = "1.0.0";

    /// <summary>
    /// Schema version of the persisted event stream format.
    /// </summary>
    public const string EventStreamSchemaVersion = "1.0";

    // Event metadata keys

    /// <summary>
    /// Metadata key holding the correlation identifier.
    /// </summary>
    public const string MetadataKeyCorrelationId = "CorrelationId";

    /// <summary>
    /// Metadata key holding the event identifier.
    /// </summary>
    public const string MetadataKeyEventId = "EventId";

    /// <summary>
    /// Metadata key holding the event timestamp.
    /// </summary>
    public const string MetadataKeyTimestamp = "Timestamp";

    /// <summary>
    /// Metadata key holding the aggregate identifier.
    /// </summary>
    public const string MetadataKeyAggregateId = "AggregateId";

    /// <summary>
    /// Metadata key holding the aggregate type name.
    /// </summary>
    public const string MetadataKeyAggregateType = "AggregateType";

    /// <summary>
    /// Metadata key holding the aggregate version.
    /// </summary>
    public const string MetadataKeyVersion = "Version";

    /// <summary>
    /// Metadata key holding the identifier of the user that caused the event.
    /// </summary>
    public const string MetadataKeyUserId = "UserId";

    /// <summary>
    /// Metadata key holding the source that produced the event.
    /// </summary>
    public const string MetadataKeySource = "Source";

    // Projection constants

    /// <summary>
    /// Prefix applied to projection snapshot keys.
    /// </summary>
    public const string ProjectionSnapshotPrefix = "snapshot_";

    /// <summary>
    /// Maximum number of attempts to rebuild a projection before giving up.
    /// </summary>
    public const int MaxProjectionRebuildAttempts = 3;

    /// <summary>
    /// Number of processed events between projection checkpoints.
    /// </summary>
    public const int ProjectionCheckpointInterval = 50;

    // Money-related constants

    /// <summary>
    /// Smallest balance an account may hold.
    /// </summary>
    public const decimal MinimumBalance = 0m;

    /// <summary>
    /// Largest balance an account may hold.
    /// </summary>
    public const decimal MaximumBalance = 9999999999.99m;

    /// <summary>
    /// Number of decimal places used for currency amounts.
    /// </summary>
    public const int CurrencyPrecision = 2;
}
