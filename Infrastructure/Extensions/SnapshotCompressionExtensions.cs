// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO.Compression;
using DotNetCqrsEventSourcing.Infrastructure.Compression;
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
}
