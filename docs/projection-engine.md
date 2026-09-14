# ProjectionEngine

The `ProjectionEngine` is a core component responsible for building read models by continuously processing events from an event source. It implements the event projector pattern in the CQRS/Event Sourcing system, maintaining checkpoints per projection to enable resumable processing and providing at-least-once delivery semantics.

## Role in Read Model Projections

In CQRS systems, read models are denormalized views optimized for query performance. The `ProjectionEngine` continuously pulls events from an `IProjectionEventSource` and applies them to update read model state. Key responsibilities include:

- Maintaining per-projection checkpoints to track processing progress
- Providing at-least-once delivery semantics (events may be processed multiple times)
- Supporting both simple event processing and transactional checkpoint advancement
- Implementing circuit breaker patterns for error resilience
- Ensuring thread-safe concurrent execution of multiple projections

## Public API

### Constructor

```csharp
public ProjectionEngine(
    ILogger<ProjectionEngine> logger,
    IProjectionEventSource eventSource)
```

**Parameters:**
- `logger`: Logger used for diagnostics and monitoring
- `eventSource`: Source the engine pulls events from (implements `IProjectionEventSource`)

**Exceptions:**
- `ArgumentNullException`: Thrown when either parameter is null

### RunAsync Overloads

The engine provides two overloads for running projections:

#### Simple Processing Delegate

```csharp
public Task RunAsync(
    string projectionName,
    Func<string, Task> processEvent,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `projectionName`: Unique name identifying the projection
- `processEvent`: Delegate that processes a single event (checkpoint always advanced after processing)
- `cancellationToken`: Token used to cancel the operation

**Returns:** A task that completes when the engine stops (typically runs indefinitely until cancelled)

**Exceptions:**
- `ArgumentNullException`: When `projectionName` or `processEvent` is null
- `ArgumentException`: When `projectionName` is empty

#### Transactional Processing Delegate

```csharp
public async Task RunAsync(
    string projectionName,
    Func<string, Task<bool>> processEventAndCheckpoint,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `projectionName`: Unique name identifying the projection
- `processEventAndCheckpoint`: Delegate that processes an event and returns `true` if checkpoint may be advanced
- `cancellationToken`: Token used to cancel the operation

**Returns:** A task that completes when the engine stops (typically runs indefinitely until cancelled)

**Exceptions:**
- `ArgumentNullException`: When `projectionName` or `processEventAndCheckpoint` is null
- `ArgumentException`: When `projectionName` is null or empty
- `InvalidOperationException`: When the projection is already running

**Behavior:**
The checkpoint is only updated when the delegate returns `true`, enabling atomic projection writes and checkpoint persistence (e.g., within a database transaction).

## Related Types

### IProjectionEventSource

```csharp
public interface IProjectionEventSource
{
    Task<string?> GetNextEventAsync(
        string projectionName,
        string? checkpoint,
        CancellationToken cancellationToken);
}
```

**Purpose:** Abstracts the event source for projections, allowing different implementations (event store, message queue, etc.)

**Parameters:**
- `projectionName`: Name of the projection requesting events
- `checkpoint`: Last processed event identifier (null when starting)
- `cancellationToken`: Token to cancel the read operation

**Returns:** The next event as a JSON string, or null when caught up (engine polls again after delay)

### ProjectionState (Internal)

Internal class tracking state per projection:
- `Name`: Projection identifier
- `Checkpoint`: Last successfully processed event
- `ConsecutiveFailures`: Count for circuit breaker logic
- `Running`: Flag preventing concurrent execution

## Usage Example

### Basic Projection Setup

```csharp
// In application startup (e.g., Program.cs)
var eventSource = new MyEventStoreEventSource(connectionString);
var projectionEngine = new ProjectionEngine(logger, eventSource);

// Start a projection
var cts = new CancellationTokenSource();
var projectionTask = projectionEngine.RunAsync(
    "AccountBalanceProjection",
    async (eventJson) =>
    {
        // Deserialize and process event
        var @event = JsonSerializer.Deserialize<DomainEvent>(eventJson);
        // Update read model...
        await _accountRepository.UpdateAsync(@event);
    },
    cts.Token);

// Later, to stop:
// cts.Cancel();
// await projectionTask;
```

### Transactional Projection (Atomic Checkpoint)

```csharp
var projectionTask = projectionEngine.RunAsync(
    "OrderSummaryProjection",
    async (eventJson) =>
    {
        using var transaction = _db.BeginTransaction();
        try
        {
            // Process event and update read model
            var @event = JsonSerializer.Deserialize<OrderEvent>(eventJson);
            await _orderRepository.UpdateSummaryAsync(@event, transaction);
            
            // If successful, advance checkpoint (return true)
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false; // Keep checkpoint, retry event
        }
    });
```

## Important Notes

### At-Least-Once Delivery
The engine follows at-least-once semantics: events may be delivered more than once. Processing delegates must be idempotent or implement their own atomic checkpoint handling to prevent duplicate processing effects.

### Checkpoint Persistence
Checkpoints are stored in-memory only. For persistent checkpoints across restarts, implementations should:
1. Store checkpoints in a durable store (database, file, etc.)
2. Initialize `IProjectionEventSource` with last known checkpoint
3. Optionally persist checkpoints when `processEventAndCheckpoint` returns true

### Circuit Breaker
After 5 consecutive failures, the engine pauses projection processing for 1 minute before resuming. This prevents tight error loops while allowing recovery from transient issues.

### Thread Safety
Multiple projections can run concurrently as each maintains independent state. However, calling `RunAsync` for the same projection name while it's already running will throw `InvalidOperationException`.

### Event Source Contract
The `IProjectionEventSource.GetNextEventAsync` method should:
- Return null when no new events are available (engine will poll after delay)
- Return the next event identifier as a string (typically JSON)
- Maintain ordering per projection name
- Be thread-safe for concurrent calls from different projections

## See Also
- `ReadModels.IProjectionEventSource`: Event source abstraction for projections
- `Application.Services.ProjectionService`: Higher-level service that may use ProjectionEngine
- `Tests.ReadModels.ProjectionEngineTests`: Unit tests demonstrating usage patterns