#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.AggregateRoots;
using Shared.Results;

/// <summary>
/// A retrieved aggregate snapshot together with the version it was captured at.
/// Provides <see cref="RestoreAggregate"/> to materialize the stored state back
/// into an <see cref="Account"/> aggregate.
/// </summary>
public sealed class AggregateSnapshot
{
    /// <summary>
    /// Current version of the snapshot envelope schema. Bump this whenever the shape
    /// of the persisted aggregate state (or the envelope itself) changes in a way that
    /// is not backward compatible with previously stored snapshots.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Serialized (JSON) aggregate state captured at snapshot time.</summary>
    public string AggregateData { get; }

    /// <summary>Aggregate version the snapshot was taken at.</summary>
    public long Version { get; }

    /// <summary>
    /// Initializes a new <see cref="AggregateSnapshot"/> handle.
    /// </summary>
    /// <param name="aggregateData">Serialized (JSON) aggregate state.</param>
    /// <param name="version">Aggregate version the snapshot was taken at.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregateData"/> is <see langword="null"/>.</exception>
    public AggregateSnapshot(string aggregateData, long version)
    {
        AggregateData = aggregateData ?? throw new ArgumentNullException(nameof(aggregateData));
        Version = version;
    }

    /// <summary>
    /// Deserializes the stored state back into an <see cref="Account"/> aggregate.
    /// </summary>
    public Account RestoreAggregate()
        => System.Text.Json.JsonSerializer.Deserialize<Account>(AggregateData) ?? new Account();

    /// <summary>
    /// Wire format persisted as <see cref="AggregateSnapshot.AggregateData"/> (Result overload) /
    /// stored via <see cref="ISnapshotService.CreateSnapshotAsync(string, long, string, CancellationToken)"/>.
    /// Stamping the schema version lets readers tell current-shape snapshots apart from
    /// stale ones written before an aggregate shape change, without ever throwing on load.
    /// </summary>
    /// <param name="SchemaVersion">Schema version the envelope was written with.</param>
    /// <param name="AggregateJson">Serialized (JSON) aggregate state.</param>
    private sealed record SnapshotEnvelope(int SchemaVersion, string AggregateJson)
    {
        /// <summary>
        /// Parameterless constructor required for deserializing legacy/malformed payloads
        /// (e.g. pre-versioning snapshots that are plain aggregate JSON, not an envelope)
        /// without throwing; missing/unknown fields simply keep their default values.
        /// </summary>
        public SnapshotEnvelope() : this(0, string.Empty) { }
    }

    /// <summary>
    /// Wraps serialized aggregate JSON in a schema-versioned envelope suitable for storage.
    /// </summary>
    /// <param name="aggregateJson">Serialized (JSON) aggregate state to wrap.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateJson"/> is <see langword="null"/> or empty.</exception>
    internal static string WrapForStorage(string aggregateJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateJson);
        return System.Text.Json.JsonSerializer.Serialize(new SnapshotEnvelope(CurrentSchemaVersion, aggregateJson));
    }

    /// <summary>
    /// Attempts to unwrap a stored envelope, returning the embedded aggregate JSON only
    /// when the envelope parses cleanly and its schema version matches
    /// <see cref="CurrentSchemaVersion"/>. Any other outcome (malformed JSON, a legacy
    /// payload that is plain aggregate data rather than an envelope, or a version stamped
    /// by an older/newer aggregate shape) yields <see langword="null"/> so the caller can
    /// discard the snapshot and rehydrate from the event stream instead of failing.
    /// </summary>
    /// <param name="storedData">Raw data persisted via <see cref="ISnapshotService.CreateSnapshotAsync(string, long, string, CancellationToken)"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="storedData"/> is <see langword="null"/> or empty.</exception>
    internal static string? TryUnwrapCurrentSchema(string storedData)
    {
        ArgumentException.ThrowIfNullOrEmpty(storedData);

        SnapshotEnvelope? envelope;
        try
        {
            envelope = System.Text.Json.JsonSerializer.Deserialize<SnapshotEnvelope>(storedData);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return envelope is { SchemaVersion: CurrentSchemaVersion, AggregateJson.Length: > 0 }
            ? envelope.AggregateJson
            : null;
    }
}

/// <summary>
/// Snapshot service interface for managing aggregate snapshots to optimize replay performance.
/// </summary>
public interface ISnapshotService
{
    Task<Result> CreateSnapshotAsync(string aggregateId, long version, string aggregateData, CancellationToken cancellationToken = default);
    Task<Result<(string AggregateData, long Version)>> GetLatestSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result> DeleteSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetSnapshotCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload that serializes the given aggregate to JSON and stores it
    /// via <see cref="CreateSnapshotAsync(string, long, string, CancellationToken)"/>.
    /// </summary>
    /// <param name="aggregate">The aggregate instance to snapshot.</param>
    /// <param name="aggregateId">The aggregate identifier the snapshot belongs to.</param>
    /// <param name="version">The aggregate version the snapshot is taken at.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is <see langword="null"/> or empty.</exception>
    Task<Result> CreateSnapshotAsync(AggregateRoot aggregate, string aggregateId, long version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        var aggregateJson = System.Text.Json.JsonSerializer.Serialize(aggregate, aggregate.GetType());
        var aggregateData = AggregateSnapshot.WrapForStorage(aggregateJson);
        return CreateSnapshotAsync(aggregateId, version, aggregateData, cancellationToken);
    }

    /// <summary>
    /// Convenience wrapper over <see cref="GetLatestSnapshotAsync"/> returning a restorable
    /// snapshot handle, or <see langword="null"/> when no snapshot exists, the payload is not
    /// parseable, or its schema version no longer matches the current aggregate shape - in all
    /// of those cases the caller is expected to discard the snapshot and rehydrate the aggregate
    /// by replaying its full event stream instead.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="aggregateId"/> is <see langword="null"/> or empty.</exception>
    async Task<AggregateSnapshot?> GetSnapshotAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        var snapshotResult = await GetLatestSnapshotAsync(aggregateId, cancellationToken);
        if (!snapshotResult.IsSuccess)
            return null;

        var aggregateJson = AggregateSnapshot.TryUnwrapCurrentSchema(snapshotResult.Data.AggregateData);
        return aggregateJson is null
            ? null
            : new AggregateSnapshot(aggregateJson, snapshotResult.Data.Version);
    }
}
