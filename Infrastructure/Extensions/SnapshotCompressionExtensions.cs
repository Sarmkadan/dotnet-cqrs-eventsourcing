#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using DotNetCqrsEventSourcing.Domain.Snapshots;
using DotNetCqrsEventSourcing.Infrastructure.Compression;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCqrsEventSourcing.Infrastructure.Extensions;

/// <summary>
/// Configuration options for snapshot compression and incremental snapshot behaviour.
/// Bind from <c>appsettings.json</c> or configure inline via the DI extension.
/// </summary>
public sealed class SnapshotCompressionOptions
{
    /// <summary>
    /// GZip compression level applied when compressing snapshot data.
    /// <see cref="CompressionLevel.Optimal"/> maximises size reduction;
    /// <see cref="CompressionLevel.Fastest"/> prioritises throughput. Default: Optimal.
    /// </summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

    /// <summary>
    /// Minimum uncompressed snapshot size in bytes before compression is applied.
    /// Snapshots smaller than this threshold are stored as-is to avoid overhead.
    /// Default: 512 bytes.
    /// </summary>
    public int MinimumSizeThreshold { get; set; } = 512;

    /// <summary>
    /// Maximum number of incremental snapshots in a chain before a new full
    /// snapshot is forced via <see cref="Domain.Snapshots.IncrementalSnapshotChain.ShouldCollapse"/>.
    /// Keeps chain traversal cost bounded. Default: 10.
    /// </summary>
    public int MaxIncrementalChainLength { get; set; } = 10;

    /// <summary>
    /// When <c>true</c>, newly created snapshots are automatically compressed
    /// if they exceed <see cref="MinimumSizeThreshold"/>. Default: <c>true</c>.
    /// </summary>
    public bool AutoCompress { get; set; } = true;
}

/// <summary>
/// <see cref="IServiceCollection"/> extension methods to register snapshot compression
/// and incremental snapshot infrastructure with a single fluent call.
/// </summary>
public static class SnapshotCompressionExtensions
{
    /// <summary>
    /// Registers <see cref="ISnapshotCompressionService"/> and
    /// <see cref="SnapshotCompressionOptions"/> with the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional delegate used to override default <see cref="SnapshotCompressionOptions"/>.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for further chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddSnapshotCompression(opt =>
    /// {
    ///     opt.Level = CompressionLevel.Fastest;
    ///     opt.MaxIncrementalChainLength = 5;
    ///     opt.AutoCompress = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSnapshotCompression(
        this IServiceCollection services,
        Action<SnapshotCompressionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SnapshotCompressionOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ISnapshotCompressionService, SnapshotCompressionService>();

        return services;
    }

    /// <summary>
    /// Compresses the snapshot's aggregate data in-place using GZip and
    /// updates the size metadata on the snapshot.
    /// </summary>
    /// <param name="compressionService">The compression service instance.</param>
    /// <param name="snapshot">The snapshot to compress.</param>
    /// <param name="level">The compression level to use.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{AggregateSnapshot}"/> indicating success or failure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="compressionService"/> or <paramref name="snapshot"/> is <see langword="null"/>.
    /// </exception>
    public static async Task<Result<AggregateSnapshot>> CompressAsync(
        this ISnapshotCompressionService compressionService,
        AggregateSnapshot snapshot,
        CompressionLevel level = CompressionLevel.Optimal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compressionService);
        ArgumentNullException.ThrowIfNull(snapshot);

        return await compressionService.CompressAsync(snapshot, level, cancellationToken);
    }

    /// <summary>
    /// Decompresses the snapshot's aggregate data and returns the original JSON string.
    /// Returns the raw data unchanged if the snapshot is not compressed.
    /// </summary>
    /// <param name="compressionService">The compression service instance.</param>
    /// <param name="snapshot">The snapshot to decompress.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{T}" where T=string/> containing the decompressed data or an error.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="compressionService"/> or <paramref name="snapshot"/> is <see langword="null"/>.
    /// </exception>
    public static async Task<Result<string>> DecompressAsync(
        this ISnapshotCompressionService compressionService,
        AggregateSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compressionService);
        ArgumentNullException.ThrowIfNull(snapshot);

        return await compressionService.DecompressAsync(snapshot, cancellationToken);
    }

    /// <summary>
    /// Gets cumulative compression statistics across all snapshots processed by the service.
    /// </summary>
    /// <param name="compressionService">The compression service instance.</param>
    /// <returns>A <see cref="SnapshotCompressionStats"/> object containing compression metrics.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="compressionService"/> is <see langword="null"/>.
    /// </exception>
    public static SnapshotCompressionStats GetStats(this ISnapshotCompressionService compressionService)
    {
        ArgumentNullException.ThrowIfNull(compressionService);

        return compressionService.GetStats();
    }
}
