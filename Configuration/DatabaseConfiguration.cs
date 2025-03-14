// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

/// <summary>
/// Database and persistence configuration for the CQRS framework.
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// Gets or sets the event store connection string.
    /// </summary>
    public string EventStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projection store connection string.
    /// </summary>
    public string ProjectionStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the snapshot store connection string.
    /// </summary>
    public string SnapshotStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of events to keep in memory cache.
    /// </summary>
    public int MaxEventsCached { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum age of cached events in seconds.
    /// </summary>
    public int CacheExpirationSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets whether to enable event compression for large events.
    /// </summary>
    public bool EnableEventCompression { get; set; } = false;

    /// <summary>
    /// Gets or sets the batch size for bulk event writes.
    /// </summary>
    public int BatchWriteSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of event reader threads.
    /// </summary>
    public int ParallelReaderCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether to automatically create snapshots.
    /// </summary>
    public bool AutoCreateSnapshots { get; set; } = true;

    /// <summary>
    /// Gets or sets the frequency of automatic snapshots (number of events).
    /// </summary>
    public int SnapshotFrequency { get; set; } = 50;

    /// <summary>
    /// Gets or sets the minimum version before creating snapshots.
    /// </summary>
    public long MinVersionForSnapshot { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to verify event checksums on read.
    /// </summary>
    public bool VerifyEventChecksums { get; set; } = true;

    /// <summary>
    /// Gets or sets the retention policy for old events.
    /// </summary>
    public EventRetentionPolicy RetentionPolicy { get; set; } = EventRetentionPolicy.Infinite;

    /// <summary>
    /// Gets or sets the number of days to retain events (for Limited policy).
    /// </summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public bool Validate()
    {
        if (MaxEventsCached <= 0)
            return false;

        if (CacheExpirationSeconds <= 0)
            return false;

        if (BatchWriteSize <= 0)
            return false;

        if (ParallelReaderCount <= 0)
            return false;

        if (SnapshotFrequency <= 0)
            return false;

        if (MinVersionForSnapshot < 0)
            return false;

        if (RetentionDays < 0)
            return false;

        return true;
    }

    /// <summary>
    /// Create a copy of this configuration.
    /// </summary>
    public DatabaseConfiguration Clone()
    {
        return new DatabaseConfiguration
        {
            EventStoreConnectionString = EventStoreConnectionString,
            ProjectionStoreConnectionString = ProjectionStoreConnectionString,
            SnapshotStoreConnectionString = SnapshotStoreConnectionString,
            MaxEventsCached = MaxEventsCached,
            CacheExpirationSeconds = CacheExpirationSeconds,
            EnableEventCompression = EnableEventCompression,
            BatchWriteSize = BatchWriteSize,
            ParallelReaderCount = ParallelReaderCount,
            AutoCreateSnapshots = AutoCreateSnapshots,
            SnapshotFrequency = SnapshotFrequency,
            MinVersionForSnapshot = MinVersionForSnapshot,
            VerifyEventChecksums = VerifyEventChecksums,
            RetentionPolicy = RetentionPolicy,
            RetentionDays = RetentionDays
        };
    }
}

/// <summary>
/// Event retention policy options.
/// </summary>
public enum EventRetentionPolicy
{
    /// <summary>
    /// Keep all events indefinitely.
    /// </summary>
    Infinite = 0,

    /// <summary>
    /// Retain events for a limited period.
    /// </summary>
    Limited = 1,

    /// <summary>
    /// Retain only snapshots and recent events.
    /// </summary>
    Snapshots = 2,

    /// <summary>
    /// Archive old events to cold storage.
    /// </summary>
    Archive = 3
}
