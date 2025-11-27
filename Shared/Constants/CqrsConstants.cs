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
    public const int DefaultSnapshotFrequency = 10;
    public const int MaxEventsPerBatch = 1000;
    public const int EventReplayBatchSize = 100;

    // Timeout configurations
    public const int DefaultCommandTimeoutSeconds = 30;
    public const int DefaultQueryTimeoutSeconds = 60;
    public const int DefaultEventPublishTimeoutSeconds = 15;

    // Version and metadata
    public const string FrameworkVersion = "1.0.0";
    public const string EventStreamSchemaVersion = "1.0";

    // Event metadata keys
    public const string MetadataKeyCorrelationId = "CorrelationId";
    public const string MetadataKeyEventId = "EventId";
    public const string MetadataKeyTimestamp = "Timestamp";
    public const string MetadataKeyAggregateId = "AggregateId";
    public const string MetadataKeyAggregateType = "AggregateType";
    public const string MetadataKeyVersion = "Version";
    public const string MetadataKeyUserId = "UserId";
    public const string MetadataKeySource = "Source";

    // Projection constants
    public const string ProjectionSnapshotPrefix = "snapshot_";
    public const int MaxProjectionRebuildAttempts = 3;
    public const int ProjectionCheckpointInterval = 50;

    // Money-related constants
    public const decimal MinimumBalance = 0m;
    public const decimal MaximumBalance = 9999999999.99m;
    public const int CurrencyPrecision = 2;
}
