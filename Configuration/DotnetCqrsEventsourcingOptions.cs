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
public sealed class DotnetCqrsEventsourcingOptions
{
    /// <summary>
    /// Configuration section name used when binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "DotnetCqrsEventsourcing";

    /// <summary>
    /// Connection string for the event store database.
    /// This is where all domain events are persisted.
    /// </summary>
    [Required(ErrorMessage = "EventStoreConnectionString is required")]
    [MinLength(10, ErrorMessage = "EventStoreConnectionString must be at least 10 characters")]
    public string EventStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for the projection store database.
    /// This is where read models are stored for query optimization.
    /// </summary>
    [Required(ErrorMessage = "ProjectionStoreConnectionString is required")]
    [MinLength(10, ErrorMessage = "ProjectionStoreConnectionString must be at least 10 characters")]
    public string ProjectionStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for the snapshot store database.
    /// This is where aggregate snapshots are persisted to optimize replay performance.
    /// </summary>
    [Required(ErrorMessage = "SnapshotStoreConnectionString is required")]
    [MinLength(10, ErrorMessage = "SnapshotStoreConnectionString must be at least 10 characters")]
    public string SnapshotStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of events to keep in memory cache.
    /// Higher values improve performance for frequently accessed aggregates but increase memory usage.
    /// </summary>
    [Range(1, 1000000, ErrorMessage = "MaxEventsCached must be between 1 and 1,000,000")]
    public int MaxEventsCached { get; set; } = 10000;

    /// <summary>
    /// Maximum age of cached events in seconds.
    /// Events older than this will be evicted from cache.
    /// Set to 0 to disable caching.
    /// </summary>
    [Range(0, 86400, ErrorMessage = "CacheExpirationSeconds must be between 0 and 86400 (24 hours)")]
    public int CacheExpirationSeconds { get; set; } = 3600;

    /// <summary>
    /// Enable event compression for large events.
    /// When enabled, events are compressed before storage to reduce database size.
    /// </summary>
    public bool EnableEventCompression { get; set; } = false;

    /// <summary>
    /// Batch size for bulk event writes.
    /// Larger batches improve write performance but increase memory usage during writes.
    /// </summary>
    [Range(1, 10000, ErrorMessage = "BatchWriteSize must be between 1 and 10,000")]
    public int BatchWriteSize { get; set; } = 100;

    /// <summary>
    /// Number of parallel event reader threads.
    /// Controls how many events can be read concurrently for better throughput.
    /// Defaults to the number of available processors.
    /// </summary>
    [Range(1, 64, ErrorMessage = "ParallelReaderCount must be between 1 and 64")]
    public int ParallelReaderCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Automatically create snapshots when <see cref="SnapshotFrequency"/> threshold is reached.
    /// When false, snapshots must be created manually.
    /// </summary>
    public bool AutoCreateSnapshots { get; set; } = true;

    /// <summary>
    /// Frequency of automatic snapshots (number of events).
    /// After this many events, a snapshot will be automatically created if <see cref="AutoCreateSnapshots"/> is true.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "SnapshotFrequency must be between 1 and 1,000")]
    public int SnapshotFrequency { get; set; } = 50;

    /// <summary>
    /// Minimum version before creating snapshots.
    /// Snapshots will only be created for aggregates that have reached this version.
    /// </summary>
    [Range(0, 1000000, ErrorMessage = "MinVersionForSnapshot must be between 0 and 1,000,000")]
    public long MinVersionForSnapshot { get; set; } = 10;

    /// <summary>
    /// Verify event checksums on read.
    /// When enabled, validates event integrity to detect data corruption.
    /// Disable only for performance testing.
    /// </summary>
    public bool VerifyEventChecksums { get; set; } = true;

    /// <summary>
    /// Retention policy for old events.
    /// </summary>
    public EventRetentionPolicy RetentionPolicy { get; set; } = EventRetentionPolicy.Infinite;

    /// <summary>
    /// Days to retain events when <see cref="RetentionPolicy"/> is set to <see cref="EventRetentionPolicy.Limited"/>.
    /// Events older than this will be automatically removed.
    /// </summary>
    [Range(1, 3650, ErrorMessage = "RetentionDays must be between 1 and 3,650 (10 years)")]
    public int RetentionDays { get; set; } = 365;
}
