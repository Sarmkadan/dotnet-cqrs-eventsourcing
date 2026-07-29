using System.ComponentModel.DataAnnotations;

namespace DotNetCqrsEventSourcing.Configuration;

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

/// <summary>
/// Configuration options for the CQRS and Event Sourcing framework.
/// </summary>
public sealed record DotnetCqrsEventsourcingOptions : IValidatableOptions
{
    /// <summary>
    /// Configuration section name used when binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "DotnetCqrsEventsourcing";

    /// <summary>
    /// Connection string for the event store database.
    /// This is where all domain events are persisted.
    /// </summary>
    public string EventStoreConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Connection string for the projection store database.
    /// This is where read models are stored for query optimization.
    /// </summary>
    public string ProjectionStoreConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Connection string for the snapshot store database.
    /// This is where aggregate snapshots are persisted to optimize replay performance.
    /// </summary>
    public string SnapshotStoreConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of events to keep in memory cache.
    /// Higher values improve performance for frequently accessed aggregates but increase memory usage.
    /// </summary>
    public int MaxEventsCached { get; init; } = 10000;

    /// <summary>
    /// Maximum age of cached events in seconds.
    /// Events older than this will be evicted from cache.
    /// Set to 0 to disable caching.
    /// </summary>
    public int CacheExpirationSeconds { get; init; } = 3600;

    /// <summary>
    /// Enable event compression for large events.
    /// When enabled, events are compressed before storage to reduce database size.
    /// </summary>
    public bool EnableEventCompression { get; init; } = false;

    /// <summary>
    /// Batch size for bulk event writes.
    /// Larger batches improve write performance but increase memory usage during writes.
    /// </summary>
    public int BatchWriteSize { get; init; } = 100;

    /// <summary>
    /// Number of parallel event reader threads.
    /// Controls how many events can be read concurrently for better throughput.
    /// Defaults to the number of available processors.
    /// </summary>
    public int ParallelReaderCount { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Automatically create snapshots when <see cref="SnapshotFrequency"/> threshold is reached.
    /// When false, snapshots must be created manually.
    /// </summary>
    public bool AutoCreateSnapshots { get; init; } = true;

    /// <summary>
    /// Frequency of automatic snapshots (number of events).
    /// After this many events, a snapshot will be automatically created if <see cref="AutoCreateSnapshots"/> is true.
    /// </summary>
    public int SnapshotFrequency { get; init; } = 50;

    /// <summary>
    /// Minimum version before creating snapshots.
    /// Snapshots will only be created for aggregates that have reached this version.
    /// </summary>
    public long MinVersionForSnapshot { get; init; } = 10;

    /// <summary>
    /// Verify event checksums on read.
    /// When enabled, validates event integrity to detect data corruption.
    /// Disable only for performance testing.
    /// </summary>
    public bool VerifyEventChecksums { get; init; } = true;

    /// <summary>
    /// Retention policy for old events.
    /// </summary>
    public EventRetentionPolicy RetentionPolicy { get; init; } = EventRetentionPolicy.Infinite;

    /// <summary>
    /// Days to retain events when <see cref="RetentionPolicy"/> is set to <see cref="EventRetentionPolicy.Limited"/>.
    /// Events older than this will be automatically removed.
    /// </summary>
    public int RetentionDays { get; init; } = 365;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if any option is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any option is out of valid range.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EventStoreConnectionString) || EventStoreConnectionString.Length < 10)
        {
            throw new ArgumentException("EventStoreConnectionString is required and must be at least 10 characters.", nameof(EventStoreConnectionString));
        }

        if (string.IsNullOrWhiteSpace(ProjectionStoreConnectionString) || ProjectionStoreConnectionString.Length < 10)
        {
            throw new ArgumentException("ProjectionStoreConnectionString is required and must be at least 10 characters.", nameof(ProjectionStoreConnectionString));
        }

        if (string.IsNullOrWhiteSpace(SnapshotStoreConnectionString) || SnapshotStoreConnectionString.Length < 10)
        {
            throw new ArgumentException("SnapshotStoreConnectionString is required and must be at least 10 characters.", nameof(SnapshotStoreConnectionString));
        }

        if (MaxEventsCached is < 1 or > 1000000)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxEventsCached), "MaxEventsCached must be between 1 and 1,000,000.");
        }

        if (CacheExpirationSeconds is < 0 or > 86400)
        {
            throw new ArgumentOutOfRangeException(nameof(CacheExpirationSeconds), "CacheExpirationSeconds must be between 0 and 86400.");
        }

        if (BatchWriteSize is < 1 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchWriteSize), "BatchWriteSize must be between 1 and 10,000.");
        }

        if (ParallelReaderCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(ParallelReaderCount), "ParallelReaderCount must be between 1 and 64.");
        }

        if (SnapshotFrequency is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(SnapshotFrequency), "SnapshotFrequency must be between 1 and 1,000.");
        }

        if (MinVersionForSnapshot is < 0 or > 1000000)
        {
            throw new ArgumentOutOfRangeException(nameof(MinVersionForSnapshot), "MinVersionForSnapshot must be between 0 and 1,000,000.");
        }

        if (RetentionDays is < 1 or > 3650)
        {
            throw new ArgumentOutOfRangeException(nameof(RetentionDays), "RetentionDays must be between 1 and 3,650.");
        }
    }
}
