# ProjectionService

`ProjectionService` is responsible for managing read-model projections in a CQRS/Event Sourcing architecture. It provides methods to update, rebuild, and retrieve projections derived from domain events, ensuring the query side remains eventually consistent with the event store.

## API

### public ProjectionService

Constructor. Initializes a new instance of the service with the necessary dependencies for accessing the event store and projection storage.

### public async Task<Result> UpdateProjectionAsync

Applies pending events for a single aggregate to its corresponding projection, bringing it up to date without a full rebuild.

- **Parameters:** Aggregate identifier and projection type information (inferred from typical CQRS update signatures).
- **Returns:** A `Result` indicating success or failure of the incremental update.
- **Throws:** May throw if the underlying event store or projection storage is unavailable.

### public async Task<Result> RebuildProjectionAsync

Drops the existing projection for a specific aggregate and replays all historical events from the event store to reconstruct it from scratch.

- **Parameters:** Aggregate identifier and projection type information.
- **Returns:** A `Result` indicating success or failure of the rebuild operation.
- **Throws:** May throw if the event stream cannot be read or the projection cannot be written.

### public async Task<Result> RebuildAllProjectionsAsync

Rebuilds every projection managed by the service by replaying the entire event store. This is typically used after schema changes or data corruption recovery.

- **Parameters:** None.
- **Returns:** A `Result` indicating overall success or failure.
- **Throws:** May throw if the event store is inaccessible or if any individual projection rebuild fails critically.

### public async Task<Result<Dictionary<string, object>>> GetProjectionAsync

Retrieves the current state of a single projection as a dictionary of property names to values.

- **Parameters:** Aggregate identifier and projection type.
- **Returns:** A `Result<Dictionary<string, object>>` containing the projection data on success, or an error result if the projection does not exist or cannot be accessed.
- **Throws:** May throw for storage-level connectivity issues.

### public async Task<Result<List<Dictionary<string, object>>>> GetAllProjectionsAsync

Retrieves all projections of a given type, returning each as a dictionary.

- **Parameters:** Projection type identifier.
- **Returns:** A `Result<List<Dictionary<string, object>>>` with the collection of projection snapshots, or an error result on failure.
- **Throws:** May throw if the projection storage is unavailable.

### public async Task<AccountProjectionSummary> BuildProjectionAsync

Constructs and returns a strongly-typed `AccountProjectionSummary` projection for a specific account aggregate by applying its event stream.

- **Parameters:** Account aggregate identifier.
- **Returns:** A fully materialized `AccountProjectionSummary` instance.
- **Throws:** May throw if the event stream is missing or the projection cannot be assembled.

## Usage

### Incremental Update After Command Handling

```csharp
var projectionService = new ProjectionService(eventStore, projectionStore);

// After successfully handling a command and persisting events,
// update the projection incrementally.
Result updateResult = await projectionService.UpdateProjectionAsync(
    aggregateId: accountId,
    projectionType: "AccountSummary");

if (updateResult.IsFailure)
{
    _logger.LogError("Projection update failed: {Error}", updateResult.Error);
}
```

### Full Rebuild for Recovery

```csharp
var projectionService = new ProjectionService(eventStore, projectionStore);

// Rebuild a specific projection after detecting inconsistency.
Result rebuildResult = await projectionService.RebuildProjectionAsync(
    aggregateId: accountId,
    projectionType: "AccountSummary");

if (rebuildResult.IsSuccess)
{
    var projection = await projectionService.GetProjectionAsync(
        aggregateId: accountId,
        projectionType: "AccountSummary");

    var summary = projection.Value;
    Console.WriteLine($"Rebuilt projection balance: {summary["Balance"]}");
}
```

## Notes

- **Eventual Consistency:** `UpdateProjectionAsync` applies only new events since the last checkpoint. If events are missed due to concurrency or failure, the projection may drift; use `RebuildProjectionAsync` to correct it.
- **Long-Running Operations:** `RebuildAllProjectionsAsync` replays the entire event history and may take considerable time and resources. It should be scheduled during maintenance windows and monitored for completion.
- **Thread Safety:** The service itself does not guarantee thread-safe access to individual projections. Concurrent calls to `UpdateProjectionAsync` for the same aggregate may interleave; external synchronization should be applied if simultaneous updates are possible.
- **Result Failures:** All `Result`-returning methods communicate failure through the result object rather than throwing exceptions for domain-level errors (e.g., missing projection). Exceptions are reserved for infrastructure failures such as storage unavailability.
- **Dictionary Projections:** `GetProjectionAsync` and `GetAllProjectionsAsync` return loosely-typed dictionaries. Callers must know the expected keys and perform their own casting. The strongly-typed `BuildProjectionAsync` offers a type-safe alternative for known projection shapes.
