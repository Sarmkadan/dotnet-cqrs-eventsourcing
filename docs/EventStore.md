# EventStore

`EventStore` is the central persistence abstraction for domain events in the `dotnet-cqrs-eventsourcing` framework. It provides a unified interface for atomically appending single or multiple events to an aggregate stream, retrieving event streams with optional version-based slicing, replaying events into projections or read models, and querying events by type or partition key. All public methods return a `Result` or `Result<T>` monad, ensuring that callers handle both success and failure paths explicitly without relying on exceptions for control flow.

## API

### `public EventStore`
The constructor. Initializes a new instance of the event store with the underlying storage provider and serialization configuration. Exact parameters depend on the concrete implementation injected via dependency injection; the public surface guarantees that an instance is ready to accept the operations described below.

### `public async Task<Result> AppendEventAsync`
Appends a single domain event to the event stream identified by the aggregate’s unique identifier and expected version.

- **Parameters:**  
  The event object (typically a `DomainEvent` derivative), the aggregate identifier, and the expected current version of the aggregate for optimistic concurrency control.
- **Returns:**  
  `Result` indicating success or a failure describing the reason (e.g., concurrency conflict, serialization error, storage unavailability).
- **Throws:**  
  Does not throw by design; all errors are captured in the `Result`.

### `public async Task<Result> AppendEventsAsync`
Atomically appends a batch of domain events to a single aggregate stream.

- **Parameters:**  
  A collection of events, the aggregate identifier, and the expected aggregate version before any of the events are applied.
- **Returns:**  
  `Result` indicating success only if the entire batch is written atomically. On failure, no partial writes are persisted.
- **Throws:**  
  Does not throw; failures are returned inside the `Result`.

### `public async Task<Result<List<DomainEvent>>> GetEventStreamAsync`
Retrieves the complete ordered event stream for a given aggregate from the first event onward.

- **Parameters:**  
  The aggregate identifier.
- **Returns:**  
  `Result<List<DomainEvent>>` containing the full list of events in ascending version order, or a failure if the stream does not exist or cannot be read.
- **Throws:**  
  Does not throw.

### `public async Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync`
Retrieves a slice of the event stream starting from a specific version (inclusive) up to the latest event.

- **Parameters:**  
  The aggregate identifier and the starting version number.
- **Returns:**  
  `Result<List<DomainEvent>>` with events whose version is greater than or equal to the requested starting version. An empty list is returned if the starting version exceeds the current aggregate version. Failure result if the stream cannot be accessed.
- **Throws:**  
  Does not throw.

### `public async Task<Result<long>> GetAggregateVersionAsync`
Returns the current version number of an aggregate, which corresponds to the highest event version persisted in its stream.

- **Parameters:**  
  The aggregate identifier.
- **Returns:**  
  `Result<long>` with the current version, or zero if no events exist for the aggregate. Failure result when the store is unreachable.
- **Throws:**  
  Does not throw.

### `public async Task<Result> ReplayEventsAsync`
Replays all events (or a filtered subset) through a provided projector or handler, typically used to rebuild read models or projections.

- **Parameters:**  
  A replay target (e.g., a projection instance) and optional filters such as event type or partition key range.
- **Returns:**  
  `Result` indicating successful replay completion or a failure detailing the error (e.g., projector exception, storage read failure).
- **Throws:**  
  Does not throw; projector exceptions are caught and surfaced in the `Result`.

### `public async Task<Result<List<DomainEvent>>> GetEventsByTypeAsync`
Queries events across all aggregates filtered by a specific event type name.

- **Parameters:**  
  The fully qualified event type name (or a discriminator string) and optional pagination or date-range parameters.
- **Returns:**  
  `Result<List<DomainEvent>>` containing matching events in chronological order. An empty list is returned when no events match. Failure result on query execution errors.
- **Throws:**  
  Does not throw.

### `public async Task<Result<int>> GetEventCountAsync`
Returns the total count of events stored, optionally scoped to an aggregate or event type.

- **Parameters:**  
  Optional aggregate identifier and/or event type filter. When called without filters, returns the global event count.
- **Returns:**  
  `Result<int>` with the count. Failure result if the underlying store cannot compute the count.
- **Throws:**  
  Does not throw.

### `public async Task<Result<List<DomainEvent>>> GetEventsByPartitionKeyAsync`
Retrieves events assigned to a specific partition key, enabling efficient querying in partitioned storage layouts.

- **Parameters:**  
  The partition key string and optional pagination parameters.
- **Returns:**  
  `Result<List<DomainEvent>>` with events belonging to that partition, ordered by their global position or timestamp. Empty list when no events exist for the partition. Failure result on storage errors.
- **Throws:**  
  Does not throw.

## Usage

### Example 1: Appending an event and reading the updated stream

```csharp
var store = serviceProvider.GetRequiredService<EventStore>();

// Append a single event with optimistic concurrency check
var newEvent = new OrderPlacedEvent(orderId, customerId, total);
var appendResult = await store.AppendEventAsync(newEvent, orderId, expectedVersion: 2);

if (appendResult.IsFailure)
{
    _logger.LogError("Failed to append event: {Reason}", appendResult.Error);
    return;
}

// Retrieve the full stream to rebuild aggregate state
var streamResult = await store.GetEventStreamAsync(orderId);
if (streamResult.IsSuccess)
{
    var order = OrderAggregate.Rehydrate(streamResult.Value);
    // Continue processing with the up-to-date aggregate
}
```

### Example 2: Replaying events to rebuild a projection

```csharp
var store = serviceProvider.GetRequiredService<EventStore>();
var projection = new ActiveOrdersProjection();

// Replay all OrderPlacedEvent and OrderCancelledEvent events
var replayResult = await store.ReplayEventsAsync(
    projection,
    eventTypes: new[] { "OrderPlacedEvent", "OrderCancelledEvent" }
);

if (replayResult.IsFailure)
{
    _logger.LogError("Projection replay failed: {Reason}", replayResult.Error);
    return;
}

// Projection now contains the reconstructed view
var activeCount = await store.GetEventCountAsync(eventType: "OrderPlacedEvent");
Console.WriteLine($"Total OrderPlaced events: {activeCount.Value}");
```

## Notes

- **Optimistic concurrency:** `AppendEventAsync` and `AppendEventsAsync` enforce expected version checks. If the aggregate has been modified concurrently, the returned `Result` will indicate a version conflict. Callers must retry by rehydrating the latest stream and re-attempting the operation.
- **Atomic batch writes:** When using `AppendEventsAsync`, either all events in the batch are persisted or none are. Partial failures are never visible to subsequent readers.
- **Empty streams:** `GetEventStreamAsync` and `GetEventStreamFromVersionAsync` return a success result with an empty list when no events exist, rather than a failure. `GetAggregateVersionAsync` returns zero in this case.
- **Replay safety:** `ReplayEventsAsync` catches exceptions thrown by the projector and wraps them in a failure result. The underlying event store read operation is not affected by projector failures; a failed replay can be retried without data loss.
- **Thread safety:** The public methods are designed for concurrent use. The underlying storage provider determines the exact thread-safety guarantees; typically, append operations are linearized per aggregate stream, while read and replay operations can proceed concurrently with writes.
- **Pagination:** Methods that return potentially large lists (`GetEventsByTypeAsync`, `GetEventsByPartitionKeyAsync`) should be called with pagination parameters when available in the concrete implementation to avoid excessive memory consumption. The signatures shown here represent the minimal public contract; overloads with pagination tokens or limits may exist in the full API surface.
- **Partition key semantics:** `GetEventsByPartitionKeyAsync` relies on the storage layout assigning events to partitions. The partition key is typically derived from aggregate identifiers or explicit sharding strategies and is set at append time. Querying by partition key without an exact match may return events from multiple aggregates that share the same partition.
