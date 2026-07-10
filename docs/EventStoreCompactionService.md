# EventStoreCompactionService

`EventStoreCompactionService` performs compaction on event streams by removing redundant or obsolete events up to a specified version, reducing storage footprint while preserving aggregate state integrity. It operates against an underlying event store and returns structured results indicating how many events were compacted and the resulting stream version.

## API

### EventStoreCompactionService
Constructor. Initializes a new instance of the service with the required dependencies for accessing the event store and applying compaction logic.

### CompactAsync
```csharp
public async Task<Result<CompactionResult>> CompactAsync(string streamName, CancellationToken cancellationToken = default)
```
Compacts a single named stream by removing all events that are safe to eliminate based on the current aggregate state. Returns a `Result<CompactionResult>` containing the number of events removed and the stream version after compaction. The operation is cancelled if `cancellationToken` is signaled.

### CompactToVersionAsync
```csharp
public async Task<Result<CompactionResult>> CompactToVersionAsync(string streamName, int targetVersion, CancellationToken cancellationToken = default)
```
Compacts the specified stream up to the given `targetVersion`, preserving events at or above that version while removing redundant earlier events where possible. Returns a `Result<CompactionResult>` with the count of removed events and the resulting stream version. Throws an `ArgumentOutOfRangeException` if `targetVersion` is negative or exceeds the current stream version. The operation respects `cancellationToken`.

### CompactAllAsync
```csharp
public async Task<Result<IReadOnlyList<CompactionResult>>> CompactAllAsync(CancellationToken cancellationToken = default)
```
Compacts all streams in the event store. Returns a `Result<IReadOnlyList<CompactionResult>>` where each entry corresponds to one stream and reports the number of events removed and the post-compaction version. Streams that fail compaction individually may cause the entire operation to return a failure result, depending on implementation policy. The operation is cancelled if `cancellationToken` is signaled.

## Usage

### Compacting a single stream to the latest safe version
```csharp
var compactionService = new EventStoreCompactionService(eventStore, snapshotProvider);
var result = await compactionService.CompactAsync("order-12345", CancellationToken.None);

if (result.IsSuccess)
{
    Console.WriteLine($"Removed {result.Value.RemovedEventCount} events. Stream now at version {result.Value.StreamVersion}.");
}
else
{
    Console.WriteLine($"Compaction failed: {result.Error}");
}
```

### Compacting all streams with a target version limit
```csharp
var compactionService = new EventStoreCompactionService(eventStore, snapshotProvider);
int keepFromVersion = 10;

var results = await compactionService.CompactAllAsync(CancellationToken.None);

if (results.IsSuccess)
{
    foreach (var compactionResult in results.Value)
    {
        Console.WriteLine($"Stream compacted: removed {compactionResult.RemovedEventCount} events, version {compactionResult.StreamVersion}");
    }
}
else
{
    Console.WriteLine($"Bulk compaction failed: {results.Error}");
}
```

## Notes

- Compaction is an irreversible operation. Once events are removed, they cannot be recovered through the event store API.
- `CompactToVersionAsync` requires the caller to know a valid target version; passing a version greater than the current stream version will result in an exception.
- The service does not guarantee atomicity across multiple streams when using `CompactAllAsync`. A failure partway through may leave some streams compacted and others untouched.
- Thread-safety is not guaranteed. Concurrent calls to compact the same stream from different threads may produce unpredictable results or corrupt the stream. Callers must serialize access per stream.
- Cancellation via `cancellationToken` may leave a stream in a partially compacted state if the underlying store does not support transactional compaction. Verify the store's consistency guarantees before relying on cancellation safety.
