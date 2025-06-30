# ReadModelProjectionEngine

The `ReadModelProjectionEngine` manages the lifecycle and execution of projections that materialize read models from an event store. It coordinates checkpoint tracking, dispatches stored events to registered projection handlers, and supports both targeted and full rebuild operations. The engine is designed to be disposed when no longer needed, releasing any held resources.

## API

### `ReadModelProjectionEngine`

Constructs a new instance of the engine. The specific constructor parameters are determined by the underlying implementation and typically include an event store connection, projection registrations, and checkpoint storage configuration.

### `async Task<Result<ProjectionRebuildResult>> RebuildAsync`

Rebuilds a single projection identified by its checkpoint or projection key.

- **Parameters:** The projection identifier (exact parameter name depends on implementation overloads, typically a projection name or type key).
- **Returns:** A `Result<ProjectionRebuildResult>` indicating success or failure. On success, the result contains details about the rebuild operation, such as the number of events processed and the final checkpoint position.
- **Exceptions:** May throw if the underlying event store connection is unavailable or if the projection handler itself throws an unhandled exception during dispatch.

### `async Task<Result<IReadOnlyList<ProjectionRebuildResult>>> RebuildAllAsync`

Rebuilds all registered projections from their respective checkpoints (or from the beginning if no checkpoint exists).

- **Parameters:** None.
- **Returns:** A `Result<IReadOnlyList<ProjectionRebuildResult>>` containing a list of rebuild outcomes, one per projection. The overall result is a failure if any individual projection rebuild fails critically.
- **Exceptions:** May throw if the event store cannot be reached or if a catastrophic failure occurs during dispatch that prevents continuation across all projections.

### `ProjectionCheckpoint? GetCheckpoint`

Retrieves the current checkpoint for a given projection.

- **Parameters:** A projection identifier (name, type, or key).
- **Returns:** The `ProjectionCheckpoint` if one has been persisted; otherwise `null`, indicating the projection has not yet processed any events or has no recorded position.
- **Exceptions:** Typically does not throw; failures may be surfaced through the return value being `null` when the checkpoint store is unreachable, depending on implementation error-handling strategy.

### `void Dispose`

Releases all resources held by the engine, including open connections to the event store, checkpoint storage handles, and any internal caches. After disposal, further calls to `RebuildAsync`, `RebuildAllAsync`, or `GetCheckpoint` are invalid and may produce undefined behavior or `ObjectDisposedException`.

## Usage

### Example 1: Rebuilding a Single Projection

```csharp
var engine = new ReadModelProjectionEngine(eventStore, checkpointStore, projections);

var rebuildResult = await engine.RebuildAsync("OrderSummaryProjection");
if (rebuildResult.IsSuccess)
{
    Console.WriteLine($"Rebuild complete. Events processed: {rebuildResult.Value.EventsProcessed}");
}
else
{
    Console.WriteLine($"Rebuild failed: {rebuildResult.Error}");
}

engine.Dispose();
```

### Example 2: Full Rebuild with Pre-Check

```csharp
using var engine = new ReadModelProjectionEngine(eventStore, checkpointStore, projections);

var checkpoint = engine.GetCheckpoint("InventoryProjection");
if (checkpoint == null)
{
    Console.WriteLine("No checkpoint found; full rebuild will start from the beginning.");
}

var allResults = await engine.RebuildAllAsync();
if (allResults.IsSuccess)
{
    foreach (var result in allResults.Value)
    {
        Console.WriteLine($"{result.ProjectionName}: {result.EventsProcessed} events, checkpoint @ {result.CheckpointPosition}");
    }
}
else
{
    Console.WriteLine($"One or more projections failed: {allResults.Error}");
}
// Engine disposed automatically by using declaration.
```

## Notes

- **Checkpoint nullability:** `GetCheckpoint` returns `null` when no checkpoint exists. Callers must handle this case to avoid assuming a projection has a recorded position.
- **Result failures:** Both `RebuildAsync` and `RebuildAllAsync` wrap outcomes in `Result`. A failure result does not necessarily mean the engine is in an unusable state; individual projection failures may leave other projections intact.
- **Thread safety:** The engine is not designed for concurrent use. `RebuildAsync` and `RebuildAllAsync` should not be invoked simultaneously from multiple threads. Sequential operations are safe.
- **Disposal:** Always call `Dispose` (or use a `using` declaration) to release resources. Rebuild operations after disposal are unsupported and may throw.
- **Partial rebuilds:** `RebuildAllAsync` processes all registered projections. If a subset must be rebuilt, use individual `RebuildAsync` calls sequentially.
