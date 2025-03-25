// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using System.Text;
using DotNetCqrsEventSourcing.Domain.Snapshots;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging;

namespace DotNetCqrsEventSourcing.Infrastructure.Compression;

/// <summary>
/// Handles GZip compression and decompression of aggregate snapshot data,
/// reducing storage footprint for large serialized aggregate states.
/// </summary>
public interface ISnapshotCompressionService
{
    /// <summary>
    /// Compresses the snapshot's <c>AggregateData</c> in-place using GZip and
    /// updates the size metadata on the snapshot. No-ops if already compressed.
    /// </summary>
    Task<Result<AggregateSnapshot>> CompressAsync(
        AggregateSnapshot snapshot,
        CompressionLevel level = CompressionLevel.Optimal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses the snapshot's <c>AggregateData</c> and returns the original
    /// JSON string. Returns the raw data unchanged if the snapshot is not compressed.
    /// </summary>
    Task<Result<string>> DecompressAsync(
        AggregateSnapshot snapshot,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns cumulative compression statistics across all snapshots processed
    /// by this service instance.
    /// </summary>
    SnapshotCompressionStats GetStats();
}

/// <summary>
/// GZip-backed implementation of <see cref="ISnapshotCompressionService"/>.
/// Thread-safe; statistics counters use Interlocked operations.
/// </summary>
public sealed class SnapshotCompressionService : ISnapshotCompressionService
{
    private readonly ILogger<SnapshotCompressionService> _logger;
    private long _totalOriginalBytes;
    private long _totalCompressedBytes;
    private int _snapshotsProcessed;

    /// <summary>Initialises the service with a required logger.</summary>
    public SnapshotCompressionService(ILogger<SnapshotCompressionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<AggregateSnapshot>> CompressAsync(
        AggregateSnapshot snapshot,
        CompressionLevel level = CompressionLevel.Optimal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.IsCompressed)
            return Result<AggregateSnapshot>.Success(snapshot);

        if (string.IsNullOrWhiteSpace(snapshot.AggregateData))
            return Result<AggregateSnapshot>.Failure("EMPTY_DATA", "Snapshot contains no data to compress");

        try
        {
            var originalBytes = Encoding.UTF8.GetBytes(snapshot.AggregateData);

            using var output = new MemoryStream();
            await using (var gzip = new GZipStream(output, level))
                await gzip.WriteAsync(originalBytes, cancellationToken);

            var compressed = output.ToArray();

            snapshot.UncompressedSize = originalBytes.Length;
            snapshot.AggregateData = Convert.ToBase64String(compressed);
            snapshot.MarkCompressed(compressed.Length);
            snapshot.ComputeChecksum();

            Interlocked.Add(ref _totalOriginalBytes, originalBytes.Length);
            Interlocked.Add(ref _totalCompressedBytes, compressed.Length);
            Interlocked.Increment(ref _snapshotsProcessed);

            _logger.LogDebug(
                "Compressed snapshot {AggregateId}@v{Version}: {Original}B -> {Compressed}B ({Ratio:F1}% reduction)",
                snapshot.AggregateId, snapshot.Version,
                originalBytes.Length, compressed.Length,
                snapshot.GetCompressionRatio());

            return Result<AggregateSnapshot>.Success(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress snapshot {AggregateId}@v{Version}",
                snapshot.AggregateId, snapshot.Version);
            return Result<AggregateSnapshot>.Failure("COMPRESSION_FAILED", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> DecompressAsync(
        AggregateSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.IsCompressed)
            return Result<string>.Success(snapshot.AggregateData);

        if (string.IsNullOrWhiteSpace(snapshot.AggregateData))
            return Result<string>.Failure("EMPTY_DATA", "Snapshot contains no compressed data");

        try
        {
            var compressedBytes = Convert.FromBase64String(snapshot.AggregateData);

            using var input = new MemoryStream(compressedBytes);
            using var output = new MemoryStream();
            await using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                await gzip.CopyToAsync(output, cancellationToken);

            var decompressed = Encoding.UTF8.GetString(output.ToArray());

            _logger.LogDebug(
                "Decompressed snapshot {AggregateId}@v{Version}: {Size}B",
                snapshot.AggregateId, snapshot.Version, decompressed.Length);

            return Result<string>.Success(decompressed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decompress snapshot {AggregateId}@v{Version}",
                snapshot.AggregateId, snapshot.Version);
            return Result<string>.Failure("DECOMPRESSION_FAILED", ex.Message);
        }
    }

    /// <inheritdoc />
    public SnapshotCompressionStats GetStats() => new()
    {
        SnapshotsProcessed = _snapshotsProcessed,
        TotalOriginalBytes = _totalOriginalBytes,
        TotalCompressedBytes = _totalCompressedBytes,
        OverallCompressionRatio = _totalOriginalBytes > 0
            ? 100.0 * (1.0 - (double)_totalCompressedBytes / _totalOriginalBytes)
            : 0.0
    };
}

/// <summary>
/// Cumulative compression statistics emitted by <see cref="SnapshotCompressionService"/>.
/// </summary>
public sealed class SnapshotCompressionStats
{
    /// <summary>Total number of snapshots that have been compressed.</summary>
    public int SnapshotsProcessed { get; init; }

    /// <summary>Sum of all original (uncompressed) byte sizes.</summary>
    public long TotalOriginalBytes { get; init; }

    /// <summary>Sum of all resulting compressed byte sizes.</summary>
    public long TotalCompressedBytes { get; init; }

    /// <summary>Overall storage saving expressed as a percentage (0–100).</summary>
    public double OverallCompressionRatio { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"SnapshotCompressionStats {{ Processed={SnapshotsProcessed}, " +
           $"Original={TotalOriginalBytes}B, Compressed={TotalCompressedBytes}B, " +
           $"Ratio={OverallCompressionRatio:F1}% }}";
}
