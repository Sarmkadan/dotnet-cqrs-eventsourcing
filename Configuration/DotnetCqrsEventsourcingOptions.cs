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
    public const string SectionName = "DotnetCqrsEventsourcing";

    [Required, MinLength(1)]
    public string EventStoreConnectionString { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string ProjectionStoreConnectionString { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string SnapshotStoreConnectionString { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxEventsCached { get; set; } = 10000;

    [Range(1, int.MaxValue)]
    public int CacheExpirationSeconds { get; set; } = 3600;

    public bool EnableEventCompression { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int BatchWriteSize { get; set; } = 100;

    [Range(1, int.MaxValue)]
    public int ParallelReaderCount { get; set; } = Environment.ProcessorCount;

    public bool AutoCreateSnapshots { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int SnapshotFrequency { get; set; } = 50;

    [Range(0, long.MaxValue)]
    public long MinVersionForSnapshot { get; set; } = 10;

    public bool VerifyEventChecksums { get; set; } = true;

    public EventRetentionPolicy RetentionPolicy { get; set; } = EventRetentionPolicy.Infinite;

    [Range(0, int.MaxValue)]
    public int RetentionDays { get; set; } = 365;
}
