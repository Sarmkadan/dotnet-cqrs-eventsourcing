// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Snapshots;

/// <summary>
/// A lightweight delta snapshot that records only the state changes that occurred
/// since a base <see cref="AggregateSnapshot"/> (or a previous incremental).
/// Chaining incrementals avoids repeatedly serialising the full aggregate state,
/// lowering both storage cost and write latency for high-frequency aggregates.
/// </summary>
public sealed class IncrementalSnapshot
{
    /// <summary>Unique identifier for this incremental snapshot.</summary>
    public string Id { get; set; }

    /// <summary>Aggregate this snapshot belongs to.</summary>
    public string AggregateId { get; set; }

    /// <summary>Fully-qualified type name of the aggregate.</summary>
    public string AggregateType { get; set; }

    /// <summary>Aggregate version captured by this delta.</summary>
    public long Version { get; set; }

    /// <summary>Version of the preceding snapshot this delta is relative to.</summary>
    public long BaseVersion { get; set; }

    /// <summary>
    /// ID of the immediately preceding snapshot — either a full
    /// <see cref="AggregateSnapshot"/> or another <see cref="IncrementalSnapshot"/>.
    /// </summary>
    public string BaseSnapshotId { get; set; }

    /// <summary>
    /// JSON-serialised dictionary mapping changed field paths to their new values.
    /// Only fields that differ from the base state are included.
    /// </summary>
    public string DeltaData { get; set; }

    /// <summary>1-based position within the incremental chain for this aggregate.</summary>
    public int SequenceNumber { get; set; }

    /// <summary>Whether <see cref="DeltaData"/> has been GZip-compressed.</summary>
    public bool IsCompressed { get; set; }

    /// <summary>UTC timestamp when this incremental snapshot was persisted.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>SHA-256 checksum for integrity verification.</summary>
    public string? Checksum { get; set; }

    /// <summary>Initialises an empty incremental snapshot with generated defaults.</summary>
    public IncrementalSnapshot()
    {
        Id = Guid.NewGuid().ToString();
        AggregateId = string.Empty;
        AggregateType = string.Empty;
        BaseSnapshotId = string.Empty;
        DeltaData = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method that creates an incremental snapshot and computes its checksum.
    /// </summary>
    /// <param name="aggregateId">Aggregate identifier.</param>
    /// <param name="aggregateType">Aggregate type name.</param>
    /// <param name="baseSnapshotId">ID of the preceding snapshot this delta is relative to.</param>
    /// <param name="baseVersion">Version of that preceding snapshot.</param>
    /// <param name="currentVersion">Current aggregate version being captured.</param>
    /// <param name="deltaData">Serialised changed-fields dictionary.</param>
    /// <param name="sequenceNumber">1-based position in the chain.</param>
    public static IncrementalSnapshot Create(
        string aggregateId,
        string aggregateType,
        string baseSnapshotId,
        long baseVersion,
        long currentVersion,
        string deltaData,
        int sequenceNumber)
    {
        var snapshot = new IncrementalSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            BaseSnapshotId = baseSnapshotId,
            BaseVersion = baseVersion,
            Version = currentVersion,
            DeltaData = deltaData,
            SequenceNumber = sequenceNumber
        };

        snapshot.ComputeChecksum();
        return snapshot;
    }

    /// <summary>Number of aggregate versions bridged by this incremental snapshot.</summary>
    public long EventDelta => Version - BaseVersion;

    /// <summary>Computes and stores a SHA-256 checksum across the key fields.</summary>
    public void ComputeChecksum()
    {
        var raw = $"{AggregateId}:{BaseVersion}:{Version}:{SequenceNumber}:{DeltaData}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        Checksum = Convert.ToBase64String(
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
    }

    /// <summary>
    /// Verifies the stored checksum; returns <c>false</c> if absent or mismatched.
    /// </summary>
    public bool VerifyChecksum()
    {
        if (string.IsNullOrEmpty(Checksum))
            return false;

        var stored = Checksum;
        ComputeChecksum();
        return Checksum == stored;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"IncrementalSnapshot {{ AggregateId={AggregateId}, " +
           $"BaseV={BaseVersion}, Version={Version}, Seq={SequenceNumber} }}";
}

/// <summary>
/// An ordered chain of <see cref="IncrementalSnapshot"/> deltas anchored to a
/// base full <see cref="AggregateSnapshot"/>, representing the complete delta
/// sequence required to reconstruct the latest aggregate state without a new
/// full snapshot.
/// </summary>
public sealed class IncrementalSnapshotChain
{
    /// <summary>Full base snapshot that anchors the chain.</summary>
    public AggregateSnapshot BaseSnapshot { get; }

    /// <summary>Ordered incremental deltas that follow the base snapshot.</summary>
    public IReadOnlyList<IncrementalSnapshot> Incrementals { get; }

    /// <summary>Latest aggregate version reachable via this chain.</summary>
    public long CurrentVersion => Incrementals.Count > 0
        ? Incrementals[^1].Version
        : BaseSnapshot.Version;

    /// <summary>Total entries in the chain (base snapshot + incrementals).</summary>
    public int Length => 1 + Incrementals.Count;

    /// <summary>
    /// Initialises the chain, sorting incrementals by <see cref="IncrementalSnapshot.SequenceNumber"/>.
    /// </summary>
    public IncrementalSnapshotChain(
        AggregateSnapshot baseSnapshot,
        IEnumerable<IncrementalSnapshot>? incrementals = null)
    {
        BaseSnapshot = baseSnapshot ?? throw new ArgumentNullException(nameof(baseSnapshot));
        Incrementals = (incrementals ?? Enumerable.Empty<IncrementalSnapshot>())
            .OrderBy(s => s.SequenceNumber)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns <c>true</c> when the number of incrementals has reached
    /// <paramref name="maxIncrementals"/>, signalling that a new full snapshot
    /// should be written to keep chain traversal cost bounded.
    /// </summary>
    public bool ShouldCollapse(int maxIncrementals = 10) =>
        Incrementals.Count >= maxIncrementals;

    /// <inheritdoc />
    public override string ToString()
        => $"IncrementalSnapshotChain {{ AggregateId={BaseSnapshot.AggregateId}, " +
           $"BaseV={BaseSnapshot.Version}, CurrentV={CurrentVersion}, Steps={Incrementals.Count} }}";
}
