# ISnapshotCompressionService

`ISnapshotCompressionService` defines the contract for compressing and decompressing aggregate snapshots within the `dotnet-cqrs-eventsourcing` framework. This service aims to optimize storage and transmission of aggregate state by reducing the payload size of event-sourced snapshots.

## API

### SnapshotCompressionService
The primary implementation of the `ISnapshotCompressionService` interface.

### CompressAsync
`public async Task<Result<AggregateSnapshot>> CompressAsync(AggregateSnapshot snapshot)`
Compresses the provided `AggregateSnapshot` instance. Returns a `Result` containing the compressed `AggregateSnapshot` upon success, or a failure result if compression fails.

### DecompressAsync
`public async Task<Result<string>> DecompressAsync(string compressedSnapshot)`
Decompresses a previously compressed snapshot string. Returns a `Result` containing the decompressed snapshot string upon success, or a failure result if decompression fails.

### GetStats
`public SnapshotCompressionStats GetStats()`
Retrieves the current performance and compression statistics for the service.

### SnapshotsProcessed
`public int SnapshotsProcessed`
The total number of snapshots processed by the service.

### TotalOriginalBytes
`public long TotalOriginalBytes`
The cumulative size of all snapshots in bytes before compression.

### TotalCompressedBytes
`public long TotalCompressedBytes`
The cumulative size of all snapshots in bytes after compression.

### OverallCompressionRatio
`public double OverallCompressionRatio`
The calculated ratio of total compressed bytes to total original bytes.

### ToString
`public override string ToString()`
Returns a string representation of the current compression statistics.

## Usage

### Example 1: Compressing a Snapshot for Storage

```csharp
public async Task SaveSnapshotAsync(AggregateSnapshot snapshot)
{
    var result = await _compressionService.CompressAsync(snapshot);
    if (result.IsSuccess)
    {
        await _repository.SaveAsync(result.Value);
    }
    else
    {
        _logger.LogError("Compression failed: {Error}", result.Error);
    }
}
```

### Example 2: Monitoring Compression Performance

```csharp
public void ReportMetrics()
{
    var stats = _compressionService.GetStats();
    _logger.LogInformation(
        "Processed {Count} snapshots. Original: {Original} bytes, Compressed: {Compressed} bytes. Ratio: {Ratio:F2}",
        _compressionService.SnapshotsProcessed,
        _compressionService.TotalOriginalBytes,
        _compressionService.TotalCompressedBytes,
        _compressionService.OverallCompressionRatio
    );
}
```

## Notes

*   **Thread Safety:** The implementation is designed to be thread-safe regarding statistics gathering. `SnapshotsProcessed`, `TotalOriginalBytes`, and `TotalCompressedBytes` are updated atomically during `CompressAsync` operations.
*   **Result Pattern:** Both `CompressAsync` and `DecompressAsync` return a `Result<T>` object. Consumers should always check the `IsSuccess` property before accessing the `Value` property to avoid `InvalidOperationException`.
*   **Empty Payloads:** Compressing or decompressing null or empty inputs may lead to failure results depending on the underlying compression algorithm implementation.
