# EventBus

A lightweight in-memory event dispatcher that allows components to publish events and subscribe/unsubscribe to specific event types without direct coupling. It supports both single-event and batch publishing, with optional persistence hooks for event-sourcing scenarios.

## Per-Aggregate Ordering Guarantees

This implementation provides **strong per-aggregate ordering guarantees**:

- **Sequential processing**: Events with the same `AggregateId` are processed in the exact order they were published
- **Parallel processing**: Events with different `AggregateId` values can be processed in parallel
- **Thread-safe**: Proper synchronization prevents race conditions during handler execution

### Why This Matters

In CQRS event-sourced systems, read models (projections) must maintain correct state by processing events in order. Without per-aggregate ordering guarantees:

- A `Deposited` event could be processed before an `AccountOpened` event
- Saga handlers could receive events out of order
- Event sourcing snapshots would be incorrect

### Implementation Details

The EventBus uses a `ConcurrentDictionary<string, SemaphoreSlim>` to maintain per-aggregate locks. Each aggregate ID gets its own `SemaphoreSlim` that ensures only one event for that aggregate is processed at a time.

**Performance characteristics:**
- Lock acquisition is O(1) average case using `ConcurrentDictionary.TryGetValue`
- Double-checked locking minimizes lock contention on the dictionary
- SemaphoreSlim allows async/await without blocking threads
- Memory overhead is minimal (one SemaphoreSlim per aggregate ID)

## API

### `EventBus`

The core class that manages event subscriptions and dispatching.

### `public Task<Result> PublishEventAsync<TEvent>(TEvent @event)`

Publishes a single event of type `TEvent` to all current subscribers.

- **Parameters**
  - `@event`: The event instance to publish.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `@event` is `null`.

### `public async Task<Result> PublishEventsAsync<TEvent>(IEnumerable<TEvent> events)`

Publishes a sequence of events of type `TEvent` to all current subscribers.

- **Parameters**
  - `events`: A collection of event instances to publish.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `events` is `null`.
  - Throws `ArgumentException` if any item in `events` is `null`.

### `public void Subscribe<TEvent>(Action<TEvent> handler)`

Registers a synchronous handler for events of type `TEvent`.

- **Parameters**
  - `handler`: The delegate to invoke when an event of type `TEvent` is published.
- **Exceptions**
  - Throws `ArgumentNullException` if `handler` is `null`.

### `public void Unsubscribe<TEvent>(Action<TEvent> handler)`

Removes a previously registered synchronous handler for events of type `TEvent`.

- **Parameters**
  - `handler`: The delegate to remove.
- **Exceptions**
  - Throws `ArgumentNullException` if `handler` is `null`.

### `public async Task<Result> PublishAndPersistAsync<TEvent>(TEvent @event)`

Publishes a single event and triggers persistence logic, typically used in event-sourced systems.

- **Parameters**
  - `@event`: The event instance to publish and persist.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `@event` is `null`.

## Usage

### Subscribing and Publishing
