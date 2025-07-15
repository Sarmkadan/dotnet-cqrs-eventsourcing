# SnapshotService

`SnapshotService` manages persistent snapshots of aggregate state in an event-sourced system. It captures the full state of an aggregate at a specific version, enabling fast reconstruction without replaying the entire event stream. The service provides asynchronous operations to create, retrieve, delete, and inspect snapshots, each returning a `Result` object that encapsulates success or failure information.

## API

### public SnapshotService

The constructor for `SnapshotService`. Initializes a new instance configured to interact with the underlying snapshot store. Specific constructor parameters depend on the implementation and may include dependencies such as a snapshot repository, serialization options, or connection details.

### public Task<Result> CreateSnapshotAsync

Creates or overwrites a snapshot for a given aggregate.

- **Parameters:** Accepts an aggregate identifier and the aggregate data to persist, along with the current aggregate version. The exact parameter list is implementation-specific (e.g., `string aggregateId, object aggregateData, long version`).
- **Returns:** `Task<Result>` — a task that completes with a `Result` indicating success or an error if the snapshot could not be written (e.g., storage failure, serialization error).
- **Throws:** Does not throw exceptions directly; errors are encapsulated in the returned `Result`. However, cancellation or invalid arguments may surface as exceptions depending on the underlying implementation.

### public Task<Result<(string AggregateData, long Version)>> GetLatestSnapshotAsync

Retrieves the most recent snapshot for a given aggregate.

- **Parameters:** Takes an aggregate identifier (e.g., `string aggregateId`).
- **Returns:** `Task<Result<(string AggregateData, long Version)>>` — a task that completes with a `Result` containing a tuple of the serialized aggregate data and the version at which the snapshot was taken. If no snapshot exists, the `Result` indicates failure or a not-found condition.
- **Throws:** Does not throw directly; errors (including “not found”) are returned via the `Result` object.

### public Task<Result> DeleteSnapshotAsync

Removes the snapshot associated with a specific aggregate.

- **Parameters:** Accepts an aggregate identifier (e.g., `string aggregateId`).
- **Returns:** `Task<Result>` — a task that completes with a `Result` indicating success or an error if the deletion fails (e.g., storage unavailability, aggregate not found).
- **Throws:** Does not throw directly; errors are surfaced through the `Result` return value.

### public Task<Result<bool>> HasSnapshotAsync

Checks whether at least one snapshot exists for a given aggregate.

- **Parameters:** Takes an aggregate identifier (e.g., `string aggregateId`).
- **Returns:** `Task<Result<bool>>` — a task that completes with a `Result` wrapping a boolean: `true` if a snapshot exists, `false` otherwise. A failure `Result` indicates the check itself could not be performed.
- **Throws:** Does not throw directly; operational errors are returned in the `Result`.

### public Task<Result<int>> GetSnapshotCountAsync

Returns the total number of snapshots stored for a given aggregate.

- **Parameters:** Accepts an aggregate identifier (e.g., `string aggregateId`).
- **Returns:** `Task<Result<int>>` — a task that completes with a `Result` containing the count of snapshots. A count of zero is a valid success result. A failure `Result` indicates the count could not be determined.
- **Throws:** Does not throw directly; errors are returned in the `Result`.

## Usage

### Example 1: Creating a snapshot and verifying its existence

```csharp
var snapshotService = new SnapshotService(/* dependencies */);
string aggregateId = "order-1234";
string aggregateData = JsonSerializer.Serialize(orderAggregate);
long currentVersion = 15;

// Persist a snapshot at the current version
Result createResult = await snapshotService.CreateSnapshotAsync(aggregateId, aggregateData, currentVersion);

if (createResult.IsSuccess)
{
    // Confirm the snapshot now exists
    Result<bool> existsResult = await snapshotService.HasSnapshotAsync(aggregateId);
    if (existsResult.IsSuccess && existsResult.Value)
    {
        Console.WriteLine("Snapshot successfully stored.");
    }
}
```

### Example 2: Loading the latest snapshot and falling back to event replay

```csharp
var snapshotService = new SnapshotService(/* dependencies */);
string aggregateId = "customer-987";

Result<(string AggregateData, long Version)> snapshotResult =
    await snapshotService.GetLatestSnapshotAsync(aggregateId);

if (snapshotResult.IsSuccess)
{
    var (data, version) = snapshotResult.Value;
    var aggregate = JsonSerializer.Deserialize<CustomerAggregate>(data);
    // Replay only events after 'version' to bring aggregate current
    Console.WriteLine($"Loaded snapshot at version {version}.");
}
else
{
    // No snapshot available — replay entire event stream
    Console.WriteLine("No snapshot found; replaying full event history.");
}
```

## Notes

- All methods return `Result` or `Result<T>` objects rather than throwing exceptions for domain or storage errors. Callers must inspect the `Result` for success or failure before accessing the contained value.
- The `GetLatestSnapshotAsync` method returns a tuple of `(string AggregateData, long Version)`. The aggregate data is serialized; callers are responsible for deserializing it into the appropriate aggregate type.
- `HasSnapshotAsync` and `GetSnapshotCountAsync` are read-only queries that do not modify snapshot state. `GetSnapshotCountAsync` returning zero is a normal, successful outcome indicating no snapshots exist.
- The service does not enforce aggregate version uniqueness internally; calling `CreateSnapshotAsync` with a version that already has a snapshot may overwrite it, depending on the underlying store implementation.
- Thread safety is the responsibility of the underlying snapshot store and the caller’s coordination. The service itself does not implement locking; concurrent calls for the same aggregate (e.g., simultaneous create and delete) can lead to race conditions unless externally synchronized.
- Cancellation of async operations is supported via standard `CancellationToken` propagation if the implementation accepts one. If a token is not exposed in the public signature, cancellation behavior depends on the internal store provider.
