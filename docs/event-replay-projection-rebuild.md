# Event Replay and Projection Rebuild Guide

This guide explains how to replay events to reconstruct aggregate state, and how to
rebuild read-model projections from scratch or from a checkpoint using the
`ReadModelProjectionEngine`.

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Replaying Events for a Single Aggregate](#replaying-events-for-a-single-aggregate)
3. [Rebuilding Projections for One Aggregate](#rebuilding-projections-for-one-aggregate)
4. [Rebuilding Projections for Many Aggregates](#rebuilding-projections-for-many-aggregates)
5. [Checkpointing](#checkpointing)
6. [Dead-Letter Store](#dead-letter-store)
7. [Configuration Reference](#configuration-reference)
8. [DI Registration](#di-registration)
9. [Common Scenarios](#common-scenarios)
10. [FAQ](#faq)

---

## Core Concepts

| Term | Description |
|------|-------------|
| **Event stream** | The ordered sequence of `DomainEvent` instances persisted for a single aggregate. |
| **Aggregate replay** | Re-applying a stored event stream to rebuild aggregate state in memory. |
| **Projection rebuild** | Re-routing all events from the event store through a projector to refresh a read model. |
| **Checkpoint** | A lightweight record of the last successfully processed event position, used to resume a rebuild or recovery without restarting from the beginning. |
| **Dead-letter store** | Durable storage for events that permanently fail projection and need manual review. |

---

## Replaying Events for a Single Aggregate

The `IEventStore` surface exposes two replay helpers:

### Replay all events

```csharp
var eventStore = serviceProvider.GetRequiredService<IEventStore>();

// Retrieves all events and logs a replay summary.
var result = await eventStore.ReplayEventsAsync("ACC-001");

if (!result.IsSuccess)
    Console.Error.WriteLine($"Replay failed: {result.ErrorMessage}");
```

### Fetch events and re-apply manually

```csharp
var streamResult = await eventStore.GetEventStreamAsync("ACC-001");

var account = new Account();          // fresh aggregate

foreach (var @event in streamResult.Data!)
{
    account.Apply(@event);            // reconstruct state step-by-step
}

Console.WriteLine($"Rebuilt balance: {account.Balance.CurrentAmount}");
```

### Replay from a specific version (e.g. after loading a snapshot)

```csharp
var snapshotResult = await snapshotService.GetLatestSnapshotAsync("ACC-001");
var snapshot = snapshotResult.Data!;

// Restore aggregate from snapshot
var account = JsonSerializer.Deserialize<Account>(snapshot.Data)!;

// Fetch only the delta events that occurred after the snapshot
var deltaResult = await eventStore.GetEventStreamFromVersionAsync(
    "ACC-001", snapshot.Version);

foreach (var @event in deltaResult.Data!)
    account.Apply(@event);

Console.WriteLine($"State up to version {account.Version}: {account.Balance.CurrentAmount}");
```

---

## Rebuilding Projections for One Aggregate

The `ReadModelProjectionEngine` fetches the full event stream from the store and
routes each event through every registered `IReadModelProjectionRunner`.

```csharp
var engine = serviceProvider.GetRequiredService<ReadModelProjectionEngine>();

var rebuildResult = await engine.RebuildAsync("ACC-001");

if (rebuildResult.IsSuccess)
{
    var summary = rebuildResult.Data!;
    Console.WriteLine($"Replayed : {summary.TotalEvents} event(s)");
    Console.WriteLine($"Failed   : {summary.FailedEventIds.Count} event(s)");
    Console.WriteLine($"Completed: {summary.CompletedAt:u}");
}
```

### Handling partial failures

Events that fail projection after all retry attempts are listed in
`ProjectionRebuildResult.FailedEventIds`.  Inspect them and re-process once the
underlying projector issue is resolved:

```csharp
foreach (var failedId in summary.FailedEventIds)
    Console.Error.WriteLine($"Failed event ID: {failedId}");
```

---

## Rebuilding Projections for Many Aggregates

`RebuildAllAsync` iterates a sequence of aggregate IDs and calls `RebuildAsync`
for each one.  A store-level failure stops processing immediately; per-projector
failures are captured in each `ProjectionRebuildResult`.

```csharp
// Collect all aggregate IDs you want to rebuild
var aggregateIds = new[] { "ACC-001", "ACC-002", "ACC-003" };

var allResults = await engine.RebuildAllAsync(aggregateIds);

if (!allResults.IsSuccess)
{
    Console.Error.WriteLine($"Rebuild aborted: {allResults.ErrorMessage}");
    return;
}

foreach (var r in allResults.Data!)
{
    Console.WriteLine(
        $"{r.AggregateId}: {r.TotalEvents} events, {r.FailedEventIds.Count} failures");
}
```

> **Tip:** For very large event stores, page through aggregate IDs in batches to
> limit memory pressure:
>
> ```csharp
> const int batchSize = 50;
> var ids = GetAllAggregateIds();   // your own enumeration
> foreach (var batch in ids.Chunk(batchSize))
>     await engine.RebuildAllAsync(batch, cancellationToken);
> ```

---

## Checkpointing

When `ReadModelProjectionOptions.EnableCheckpointing` is `true` (the default),
the engine writes a `ProjectionCheckpoint` every
`CheckpointInterval` events per projector.  Checkpoints let you resume a rebuild
after a crash without replaying the entire history.

### Reading the latest checkpoint

```csharp
var checkpoint = engine.GetCheckpoint("AccountReadModelProjector");

if (checkpoint is not null)
{
    Console.WriteLine($"Last event  : {checkpoint.LastEventId}");
    Console.WriteLine($"At version  : {checkpoint.LastVersion}");
    Console.WriteLine($"Recorded at : {checkpoint.RecordedAt:u}");
    Console.WriteLine($"Events seen : {checkpoint.TotalEventsProcessed}");
}
```

### Clearing checkpoints before a full rebuild

Set `ClearCheckpointsBeforeRebuild = true` in `ReadModelProjectionOptions` or
clear them manually:

```csharp
// Option A – via configuration (clears automatically on RebuildAllAsync)
services.Configure<ReadModelProjectionOptions>(o =>
{
    o.ClearCheckpointsBeforeRebuild = true;
});

// Option B – inspect then clear selectively (not exposed by default;
// extend the engine or use the Checkpoints dictionary directly).
```

---

## Dead-Letter Store

Events that exhaust all retry attempts are written to the
`IDeadLetterStore` when `EnableDeadLetterStore` is `true`.
Register your own store or use the built-in in-memory implementation:

```csharp
services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();
```

### Reading dead-letter entries

```csharp
var deadLetterStore = serviceProvider.GetRequiredService<IDeadLetterStore>();

// InMemoryDeadLetterStore exposes ReadAllAsync()
var entries = await deadLetterStore.ReadAllAsync();

foreach (var entry in entries)
{
    Console.WriteLine(
        $"[{entry.ProjectionName}] Event {entry.Event.EventId} " +
        $"failed after {entry.AttemptCount} attempt(s): {entry.ErrorMessage}");
}
```

---

## Configuration Reference

```csharp
services.Configure<ReadModelProjectionOptions>(options =>
{
    // Maximum number of projectors to run concurrently per event
    options.MaxConcurrentProjectors = 4;

    // Retry policy
    options.MaxRetryAttempts = 3;
    options.RetryBaseDelayMilliseconds = 200;   // doubles on each retry

    // Per-projector invocation timeout
    options.ProjectorTimeout = TimeSpan.FromSeconds(30);

    // Checkpointing
    options.EnableCheckpointing = true;
    options.CheckpointInterval = 100;           // write a checkpoint every 100 events
    options.ClearCheckpointsBeforeRebuild = false;

    // Dead-letter store
    options.EnableDeadLetterStore = true;
});
```

---

## DI Registration

```csharp
services.AddCqrsFramework();   // registers EventStore, EventBus, etc.

// Register one or more projection runners
services.AddSingleton<IReadModelProjectionRunner, AccountReadModelProjector>();

// Register optional dead-letter store
services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

// Register the engine (depends on the runners and options above)
services.AddSingleton<ReadModelProjectionEngine>();

// Projection options (optional – sensible defaults are provided)
services.Configure<ReadModelProjectionOptions>(o =>
{
    o.MaxRetryAttempts = 3;
    o.EnableCheckpointing = true;
});
```

---

## Common Scenarios

### Scenario 1 – Deploying a new read model

A new read model is added.  The event store already contains thousands of events.
Rebuild the new projection without touching existing ones:

```csharp
// Register only the new projector, leave existing ones running
services.AddSingleton<IReadModelProjectionRunner, NewReportingProjector>();

// After deployment, trigger a one-time rebuild
var allIds = await GetAllAggregateIdsFromEventStore();
await engine.RebuildAllAsync(allIds, stoppingToken);
```

### Scenario 2 – Fixing a projection bug

After fixing a bug in an existing projector, replay only affected aggregates:

```csharp
var affectedIds = new[] { "ACC-101", "ACC-102" };
var result = await engine.RebuildAllAsync(affectedIds, stoppingToken);
```

### Scenario 3 – Full disaster recovery

The read-model database was lost.  Rebuild everything from the event store:

```csharp
services.Configure<ReadModelProjectionOptions>(o =>
{
    o.ClearCheckpointsBeforeRebuild = true;
});

var allIds = await GetAllAggregateIdsFromEventStore();
await engine.RebuildAllAsync(allIds, stoppingToken);
```

### Scenario 4 – Aggregate state reconstruction at a point in time

Reconstruct what an account looked like after exactly 3 events:

```csharp
var streamResult = await eventStore.GetEventStreamAsync("ACC-001");
var account = new Account();

foreach (var @event in streamResult.Data!.Take(3))
    account.Apply(@event);

Console.WriteLine($"Balance after 3 events: {account.Balance.CurrentAmount}");
```

---

## FAQ

**Q: Can I replay events without rebuilding projections?**  
A: Yes. Use `eventStore.ReplayEventsAsync` or `GetEventStreamAsync` and apply events
to a fresh aggregate instance. No projectors are invoked.

**Q: What happens if a projector is not idempotent?**  
A: A rebuild will double-apply events, producing incorrect read-model state.
Design projectors to be idempotent: writing the same event twice should yield the
same result (e.g. upsert by aggregate ID + version).

**Q: How do I monitor rebuild progress?**  
A: Poll `engine.TotalEventsRouted` and `engine.Checkpoints` during a long rebuild,
or wire the engine into your observability stack via `ILogger`.

**Q: Can I run a rebuild while the application is live?**  
A: Yes. The `ReadModelProjectionEngine` is designed to run concurrently with live
event processing. Use a separate cancellation token and expect the rebuild to pick
up live events as well.

**Q: What is the difference between `ReplayEventsAsync` on `IEventStore` and `RebuildAsync` on the engine?**  
A: `IEventStore.ReplayEventsAsync` simply fetches and logs events – it does not
invoke any projectors. `ReadModelProjectionEngine.RebuildAsync` fetches the stream
*and* routes every event through all registered projectors to refresh read models.
