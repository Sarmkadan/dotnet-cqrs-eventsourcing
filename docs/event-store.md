# EventStore Class

## Purpose

The `EventStore` class is responsible for handling persistence, retrieval, and replay of domain events in the CQRS/Event Sourcing architecture. It implements the `IEventStore` interface and provides methods for appending events, retrieving event streams, and managing event-related operations.

## Public API

### Constructor

```csharp
public EventStore(IEventRepository eventRepository, ILogger<EventStore> logger, EventTypeRegistry? eventTypeRegistry = null)
```

Initializes a new instance of the EventStore class.

**Parameters:**
- `eventRepository`: The event repository used for data persistence
- `logger`: The logger instance for logging operations
- `eventTypeRegistry`: Optional event type registry for deserialization security

### Methods

#### AppendEventAsync

```csharp
public async Task<Result> AppendEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
```

Appends a single domain event to the event store.

**Parameters:**
- `@event`: The domain event to append
- `cancellationToken`: Optional cancellation token

**Returns:** A result indicating success or failure

#### AppendEventsAsync

```csharp
public async Task<Result> AppendEventsAsync(List<DomainEvent> events, CancellationToken cancellationToken = default)
```

Appends multiple domain events to the event store.

**Parameters:**
- `events`: The domain events to append
- `cancellationToken`: Optional cancellation token

**Returns:** A result indicating success or failure

#### GetEventStreamAsync

```csharp
public async Task<Result<List<DomainEvent>>> GetEventStreamAsync(string aggregateId, CancellationToken cancellationToken = default)
```

Retrieves the event stream for a given aggregate ID.

**Parameters:**
- `aggregateId`: The aggregate ID
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the list of domain events or an error

#### GetEventStreamFromVersionAsync

```csharp
public async Task<Result<List<DomainEvent>>> GetEventStreamFromVersionAsync(string aggregateId, long fromVersion, CancellationToken cancellationToken = default)
```

Retrieves the event stream for a given aggregate ID starting from a specific version (exclusive).

**Parameters:**
- `aggregateId`: The aggregate ID
- `fromVersion`: The version to start from (must be greater than zero)
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the list of domain events or an error

#### GetAggregateVersionAsync

```csharp
public async Task<Result<long>> GetAggregateVersionAsync(string aggregateId, CancellationToken cancellationToken = default)
```

Gets the current version of an aggregate.

**Parameters:**
- `aggregateId`: The aggregate ID
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the aggregate version or an error

#### ReplayEventsAsync

```csharp
public async Task<Result> ReplayEventsAsync(string aggregateId, CancellationToken cancellationToken = default)
```

Replays all events for a given aggregate ID.

**Parameters:**
- `aggregateId`: The aggregate ID
- `cancellationToken`: Optional cancellation token

**Returns:** A result indicating success or failure

#### GetEventsByTypeAsync

```csharp
public async Task<Result<List<DomainEvent>>> GetEventsByTypeAsync(string eventType, CancellationToken cancellationToken = default)
```

Retrieves events by their type.

**Parameters:**
- `eventType`: The type of the event to retrieve
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the list of domain events or an error

#### GetEventCountAsync

```csharp
public async Task<Result<int>> GetEventCountAsync(string aggregateId, CancellationToken cancellationToken = default)
```

Gets the count of events for a given aggregate ID.

**Parameters:**
- `aggregateId`: The aggregate ID
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the event count or an error

#### GetEventsByPartitionKeyAsync

```csharp
public async Task<Result<List<DomainEvent>>> GetEventsByPartitionKeyAsync(string partitionKey, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
```

Retrieves events by their partition key with pagination.

**Parameters:**
- `partitionKey`: The partition key to filter events by
- `pageNumber`: The page number to retrieve (1-based, default: 1)
- `pageSize`: The number of events per page (default: 100)
- `cancellationToken`: Optional cancellation token

**Returns:** A result containing the list of domain events or an error

## Usage Example

```csharp
// Assuming dependencies are injected via DI container
public class ExampleUsage
{
    private readonly IEventStore _eventStore;
    
    public ExampleUsage(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    public async Task HandleOrderCreatedAsync(OrderCreatedEvent orderEvent)
    {
        // Append a single event
        var appendResult = await _eventStore.AppendEventAsync(orderEvent);
        
        if (appendResult.IsSuccess)
        {
            // Retrieve the event stream for this aggregate
            var streamResult = await _eventStore.GetEventStreamAsync(orderEvent.AggregateId);
            
            if (streamResult.IsSuccess)
            {
                var events = streamResult.Data;
                // Process events...
            }
        }
    }
    
    public async Task ReplayOrderEventsAsync(string orderId)
    {
        // Replay all events for an order aggregate
        var replayResult = await _eventStore.ReplayEventsAsync(orderId);
        
        if (replayResult.IsSuccess)
        {
            // Replay successful
        }
    }
    
    public async Task<int> GetOrderEventCountAsync(string orderId)
    {
        // Get count of events for an order
        var countResult = await _eventStore.GetEventCountAsync(orderId);
        
        if (countResult.IsSuccess)
        {
            return countResult.Data;
        }
        
        return 0; // or handle error appropriately
    }
}
```

## Security Features

The EventStore implements strict allow-listing for event deserialization through the `EventTypeRegistry`. This prevents deserialization gadget vectors by only allowing registered event types to be deserialized. Unknown event types trigger an `UnknownEventTypeException` which is logged as a security violation.

## Error Handling

All methods follow a consistent error handling pattern:
- Validation exceptions are re-thrown
- Unexpected exceptions are logged and wrapped in appropriate `Result.Failure` responses
- The class uses the `Shared.Results.Result` type for consistent error handling
- Detailed logging is provided for both successful operations and failures

## Thread Safety

The EventStore class is designed to be thread-safe when used with thread-safe implementations of its dependencies (`IEventRepository`, `ILogger<EventStore>`, and `EventTypeRegistry`). The class itself does not maintain mutable state that would require synchronization.