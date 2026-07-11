# InMemoryEventRepository

`InMemoryEventRepository` is an in-memory implementation of an event store designed for use in CQRS and event-sourcing architectures. It stores event envelopes in thread-safe, concurrent collections, providing fast, non-durable persistence suitable for testing, prototyping, and scenarios where persistence is handled externally. All operations return `Result` or `Result<T>` wrappers, enabling consistent error handling without exceptions for expected failure paths.

## API

### SaveEventAsync

```csharp
public Task<Result> SaveEventAsync(EventEnvelope eventEnvelope)
```

Persists a single event envelope to the in-memory store.

- **Parameters:**
  - `eventEnvelope` — The event envelope to persist. Must not be null.
- **Returns:** A `Task<Result>` indicating success or failure. Failure occurs if the envelope is null or if an event with the same identifier already exists.
- **Exceptions:** No synchronous exceptions are thrown. Failures are communicated via the `Result` object.

### SaveEventsAsync

```csharp
public Task<Result> SaveEventsAsync(IEnumerable<EventEnvelope> eventEnvelopes)
```

Persists a batch of event envelopes atomically from the caller's perspective. Duplicate detection is performed across the entire batch and against existing events.

- **Parameters:**
  - `eventEnvelopes` — A collection of event envelopes to persist. Must not be null and must not contain null elements.
- **Returns:** A `Task<Result>` indicating success or failure. Failure occurs if the collection is null, contains null entries, or any event identifier duplicates an existing event or another event in the batch.
- **Exceptions:** No synchronous exceptions are thrown.

### GetEventsByAggregateIdAsync

```csharp
public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAsync(string aggregateId)
```

Retrieves all events for a given aggregate identifier, ordered by version ascending.

- **Parameters:**
  - `aggregateId` — The aggregate identifier. Must not be null or empty.
- **Returns:** A `Task<Result<List<EventEnvelope>>>` containing the ordered list of events, or a failure result if the aggregate ID is invalid.
- **Exceptions:** No synchronous exceptions are thrown.

### GetEventsByAggregateIdAndVersionAsync

```csharp
public Task<Result<List<EventEnvelope>>> GetEventsByAggregateIdAndVersionAsync(string aggregateId, long version)
```

Retrieves events for a given aggregate identifier starting from a specific version (inclusive), ordered by version ascending.

- **Parameters:**
  - `aggregateId` — The aggregate identifier. Must not be null or empty.
  - `version` — The minimum version to include. Must be greater than or equal to zero.
- **Returns:** A `Task<Result<List<EventEnvelope>>>` containing the filtered, ordered list of events, or a failure result for invalid arguments.
- **Exceptions:** No synchronous exceptions are thrown.

### GetEventByIdAsync

```csharp
public Task<Result<EventEnvelope>> GetEventByIdAsync(string eventId)
```

Retrieves a single event envelope by its unique event identifier.

- **Parameters:**
  - `eventId` — The unique event identifier. Must not be null or empty.
- **Returns:** A `Task<Result<EventEnvelope>>` containing the event if found, or a failure result if the ID is invalid or no event exists with that identifier.
- **Exceptions:** No synchronous exceptions are thrown.

### GetEventsByTypeAsync

```csharp
public Task<Result<List<EventEnvelope>>> GetEventsByTypeAsync(string eventType)
```

Retrieves all events of a specific event type across all aggregates, ordered by their natural insertion order.

- **Parameters:**
  - `eventType` — The fully qualified event type name. Must not be null or empty.
- **Returns:** A `Task<Result<List<EventEnvelope>>>` containing the matching events, or a failure result for invalid arguments.
- **Exceptions:** No synchronous exceptions are thrown.

### GetAggregateVersionAsync

```csharp
public Task<Result<long>> GetAggregateVersionAsync(string aggregateId)
```

Returns the current version (highest event version) for a given aggregate. Returns `0` if no events exist for the aggregate.

- **Parameters:**
  - `aggregateId` — The aggregate identifier. Must not be null or empty.
- **Returns:** A `Task<Result<long>>` containing the version number, or a failure result for invalid arguments.
- **Exceptions:** No synchronous exceptions are thrown.

### GetAllEventsAsync

```csharp
public Task<Result<List<EventEnvelope>>> GetAllEventsAsync()
```

Retrieves every event stored in the repository, ordered by insertion order.

- **Returns:** A `Task<Result<List<EventEnvelope>>>` containing all events. The result is always successful unless an internal inconsistency is detected.
- **Exceptions:** No synchronous exceptions are thrown.

### GetEventsByPartitionKeyAsync

```csharp
public Task<Result<List<EventEnvelope>>> GetEventsByPartitionKeyAsync(string partitionKey)
```

Retrieves all events associated with a specific partition key, ordered by insertion order.

- **Parameters:**
  - `partitionKey` — The partition key. Must not be null or empty.
- **Returns:** A `Task<Result<List<EventEnvelope>>>` containing the matching events, or a failure result for invalid arguments.
- **Exceptions:** No synchronous exceptions are thrown.

### DeleteEventsBeforeVersionAsync

```csharp
public Task<Result<int>> DeleteEventsBeforeVersionAsync(string aggregateId, long version)
```

Deletes all events for a given aggregate whose version is strictly less than the specified version. This supports snapshot pruning and data retention policies.

- **Parameters:**
  - `aggregateId` — The aggregate identifier. Must not be null or empty.
  - `version` — The cutoff version. Events with a version less than this value are removed.
- **Returns:** A `Task<Result<int>>` containing the number of events deleted, or a failure result for invalid arguments.
- **Exceptions:** No synchronous exceptions are thrown.

## Usage

### Example 1: Saving and Loading an Aggregate's Event Stream

```csharp
var repository = new InMemoryEventRepository();

// Create and save events for a new aggregate
var event1 = new EventEnvelope
{
    EventId = Guid.NewGuid().ToString(),
    AggregateId = "order-123",
    Version = 1,
    EventType = "OrderCreated",
    Payload = /* serialized payload */
};

var event2 = new EventEnvelope
{
    EventId = Guid.NewGuid().ToString(),
    AggregateId = "order-123",
    Version = 2,
    EventType = "OrderShipped",
    Payload = /* serialized payload */
};

Result saveResult = await repository.SaveEventsAsync(new[] { event1, event2 });
if (saveResult.IsFailure)
{
    // Handle duplicate or invalid events
}

// Retrieve the full event stream to rebuild aggregate state
Result<List<EventEnvelope>> eventsResult = await repository.GetEventsByAggregateIdAsync("order-123");
if (eventsResult.IsSuccess)
{
    foreach (var evt in eventsResult.Value)
    {
        Apply(evt); // Rehydrate aggregate
    }
}
```

### Example 2: Snapshot Pruning

```csharp
var repository = new InMemoryEventRepository();

// After building a snapshot at version 50, delete older events
Result<long> versionResult = await repository.GetAggregateVersionAsync("customer-789");
if (versionResult.IsSuccess && versionResult.Value >= 50)
{
    Result<int> deletedResult = await repository.DeleteEventsBeforeVersionAsync("customer-789", 50);
    if (deletedResult.IsSuccess)
    {
        Console.WriteLine($"Pruned {deletedResult.Value} events before version 50.");
    }
}

// Subsequent loads from version 50 onward remain valid
Result<List<EventEnvelope>> recentEvents = 
    await repository.GetEventsByAggregateIdAndVersionAsync("customer-789", 50);
```

## Notes

- **Thread safety:** All public methods are safe to call concurrently from multiple threads. Internal collections use locks or concurrent data structures to ensure consistency during reads and writes.
- **Duplicate detection:** `SaveEventAsync` and `SaveEventsAsync` enforce uniqueness by event identifier. Attempting to save an event with an existing ID results in a failure result, not an overwrite.
- **Ordering guarantees:** Events returned by aggregate-scoped queries are ordered by version ascending. Global queries (`GetAllEventsAsync`, `GetEventsByTypeAsync`, `GetEventsByPartitionKeyAsync`) return events in the order they were inserted.
- **Version numbering:** Versions are expected to be positive, monotonically increasing integers per aggregate. Gaps in version sequences are permitted; the repository does not enforce contiguity.
- **Empty results:** Queries that match no events return successful results with empty lists. `GetAggregateVersionAsync` returns `0` for aggregates with no events.
- **Argument validation:** Null or empty string arguments for identifiers and type names produce failure results. The repository does not throw `ArgumentNullException` or similar exceptions.
- **Lifecycle:** Data exists only for the lifetime of the `InMemoryEventRepository` instance. No persistence to disk or external storage occurs. All data is lost when the instance is garbage-collected or the process terminates.
