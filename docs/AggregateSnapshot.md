# AggregateSnapshot

A lightweight DTO that represents a serialized state of an event-sourced aggregate at a given point in time. Used for snapshotting aggregates to avoid replaying the entire event stream during reconstruction. Contains metadata for integrity verification and optional compression tracking.

## API

### `Id`
The unique identifier of this snapshot. Used as a primary key in storage.

### `AggregateId`
The identifier of the aggregate this snapshot represents. Must match the aggregate root's identity.

### `AggregateType`
The fully qualified type name of the aggregate (e.g., `MyApp.Domain.Models.Order`). Used to ensure correct deserialization.

### `Version`
The version of the aggregate at the time this snapshot was taken. Corresponds to the highest event sequence number applied.

### `AggregateData`
The serialized state of the aggregate, typically in JSON or binary format. May be compressed depending on `IsCompressed`.

### `CreatedAt`
The UTC timestamp when this snapshot was created. Used for ordering and retention policies.

### `Checksum`
A hash (e.g., SHA-256) of the uncompressed `AggregateData`. Used to detect corruption or tampering. May be `null` if not computed.

### `CompressedSize`
The size in bytes of the compressed `AggregateData`. Only valid when `IsCompressed` is `true`.

### `UncompressedSize`
The size in bytes of the original `AggregateData` before compression. Used to calculate compression ratio.

### `IsCompressed`
Indicates whether `AggregateData` is stored in compressed form (e.g., GZIP). Affects how `AggregateData` should be interpreted.

### `AggregateSnapshot()`
Constructs an empty snapshot. All properties must be set before use.

### `AggregateSnapshot(string id, string aggregateId, string aggregateType, long version, string aggregateData, DateTime createdAt)`
Constructs a snapshot with the provided values. `Checksum` is initially `null` and must be computed via `ComputeChecksum`.

#### Parameters
- `id`: Unique identifier for this snapshot.
- `aggregateId`: Identity of the aggregate.
- `aggregateType`: Fully qualified type name of the aggregate.
- `version`: Aggregate version at snapshot time.
- `aggregateData`: Serialized aggregate state.
- `createdAt`: UTC timestamp of creation.

### `void ComputeChecksum()`
Calculates the checksum of the uncompressed `AggregateData` and stores it in `Checksum`. Uses a cryptographic hash function (e.g., SHA-256). Throws `InvalidOperationException` if `AggregateData` is `null` or empty.

### `bool VerifyChecksum()`
Compares the stored `Checksum` against a freshly computed checksum of `AggregateData`. Returns `true` if they match; otherwise, `false`. Returns `false` if `Checksum` is `null`.

### `void MarkCompressed(int compressedSize)`
Flags this snapshot as compressed and records the compressed size. Must be called after compression is applied to `AggregateData`.

#### Parameters
- `compressedSize`: Size in bytes of the compressed data.

### `double GetCompressionRatio()`
Calculates the compression ratio as `UncompressedSize / CompressedSize`. Returns `1.0` if `IsCompressed` is `false` or sizes are invalid.

### `int GetSizeInKilobytes()`
Returns the size of `AggregateData` in kilobytes, rounded up. Uses `UncompressedSize` if `IsCompressed` is `false`; otherwise, uses `CompressedSize`.

### `override string ToString()`
Returns a human-readable summary of the snapshot, including `AggregateId`, `Version`, `CreatedAt`, and whether the data is compressed.

## Usage

### Creating and verifying a snapshot
