#nullable enable

namespace DotNetCqrsEventSourcing.Domain.Snapshots;

/// <summary>
/// Provides useful extension methods for <see cref="AggregateSnapshot"/> to simplify common snapshot operations.
/// </summary>
public static class AggregateSnapshotExtensions
{
    /// <summary>
    /// Creates a deep copy of the aggregate snapshot.
    /// </summary>
    /// <param name="snapshot">The source snapshot to copy. Cannot be <see langword="null"/>.</param>
    /// <returns>A new <see cref="AggregateSnapshot"/> instance with identical property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static AggregateSnapshot DeepCopy(this AggregateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var copy = new AggregateSnapshot(
            snapshot.AggregateId,
            snapshot.AggregateType,
            snapshot.Version,
            snapshot.AggregateData
        )
        {
            Id = snapshot.Id,
            CreatedAt = snapshot.CreatedAt,
            Checksum = snapshot.Checksum,
            CompressedSize = snapshot.CompressedSize,
            UncompressedSize = snapshot.UncompressedSize,
            IsCompressed = snapshot.IsCompressed
        };

        return copy;
    }

    /// <summary>
    /// Updates the aggregate data while maintaining version and metadata consistency.
    /// </summary>
    /// <param name="snapshot">The snapshot to update. Cannot be <see langword="null"/>.</param>
    /// <param name="newData">The new aggregate data. Cannot be <see langword="null"/>.</param>
    /// <returns>The updated snapshot (same instance for method chaining).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> or <paramref name="newData"/> is <see langword="null"/>.</exception>
    public static AggregateSnapshot WithUpdatedData(this AggregateSnapshot snapshot, string newData)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(newData);

        snapshot.AggregateData = newData;
        snapshot.UncompressedSize = newData.Length;
        snapshot.CompressedSize = newData.Length;
        snapshot.IsCompressed = false;
        snapshot.Checksum = null;

        return snapshot;
    }

    /// <summary>
    /// Determines if this snapshot is newer than another snapshot based on version and creation time.
    /// </summary>
    /// <param name="snapshot">The current snapshot. Cannot be <see langword="null"/>.</param>
    /// <param name="other">The other snapshot to compare with. Can be <see langword="null"/>.</param>
    /// <returns>True if this snapshot is newer; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static bool IsNewerThan(this AggregateSnapshot snapshot, AggregateSnapshot? other)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (other is null)
            return true;

        return snapshot.Version > other.Version
            || (snapshot.Version == other.Version && snapshot.CreatedAt > other.CreatedAt);
    }

    /// <summary>
    /// Creates a snapshot with compressed data from uncompressed data.
    /// </summary>
    /// <param name="snapshot">The snapshot to compress. Cannot be <see langword="null"/>.</param>
    /// <param name="compressor">Function that compresses the data and returns compressed size. Cannot be <see langword="null"/>.</param>
    /// <returns>The compressed snapshot (same instance for method chaining).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> or <paramref name="compressor"/> is <see langword="null"/>.</exception>
    public static AggregateSnapshot WithCompressedData(this AggregateSnapshot snapshot, Func<string, (string compressedData, int compressedSize)> compressor)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(compressor);

        var (compressedData, compressedSize) = compressor(snapshot.AggregateData);
        snapshot.AggregateData = compressedData;
        snapshot.MarkCompressed(compressedSize);
        snapshot.ComputeChecksum();

        return snapshot;
    }
}