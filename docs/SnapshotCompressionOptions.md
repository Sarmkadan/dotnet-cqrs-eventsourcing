# SnapshotCompressionOptions

Configuration options for snapshot compression behavior in event-sourced systems. Controls trade-offs between storage efficiency, compression speed, and incremental snapshot chain management.

## API

### `Level`
Gets or sets the compression level applied to snapshot data. Higher levels provide better compression ratios but require more CPU time.

- **Type**: `CompressionLevel`
- **Default**: `CompressionLevel.Optimal`
- **Remarks**: Valid values are defined in `System.IO.Compression.CompressionLevel`. Setting this to `CompressionLevel.NoCompression` disables compression entirely.

### `MinimumSizeThreshold`
Gets or sets the minimum size (in bytes) a snapshot must exceed before compression is applied. Snapshots smaller than this value are stored uncompressed regardless of `Level`.

- **Type**: `int`
- **Default**: `1024` (1 KB)
- **Remarks**: Must be a non-negative integer. Snapshots exactly equal to this threshold are compressed.

### `MaxIncrementalChainLength`
Gets or sets the maximum number of incremental snapshots allowed in a chain before forcing a full snapshot. When the chain length reaches this value, the next snapshot will be a full snapshot instead of an incremental one.

- **Type**: `int`
- **Default**: `10`
- **Remarks**: Must be a positive integer. Setting to `1` forces a full snapshot on every update.

### `AutoCompress`
Gets or sets whether snapshots should be automatically compressed when persisted. If `false`, snapshots are stored in their original form without compression.

- **Type**: `bool`
- **Default**: `true`
- **Remarks**: When `false`, `Level` and `MinimumSizeThreshold` are ignored for storage operations.

### `AddSnapshotCompression(IServiceCollection)`
Registers snapshot compression services with the dependency injection container. Includes configuration for compression providers and snapshot strategy.

- **Parameters**:
  - `services`: The `IServiceCollection` to configure.
- **Returns**: `IServiceCollection` for method chaining.
- **Remarks**: Must be called during application startup. Registers scoped services required for snapshot compression and storage.

## Usage

### Basic Configuration
