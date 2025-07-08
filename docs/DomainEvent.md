# DomainEvent

The `DomainEvent` class serves as the foundational base type for all events within the `dotnet-cqrs-eventsourcing` architecture, encapsulating the essential metadata required for event sourcing and CQRS patterns. It provides a standardized structure for tracking event identity, aggregate state, temporal occurrence, and contextual information such as user identity and correlation IDs, while offering extensibility points for custom metadata and event type resolution.

## API

### `EventId`
```csharp
public string EventId { get; set; }
```
Gets or sets the unique identifier for this specific event instance. This value distinguishes the event from all others in the event store, ensuring idempotency during replay.

### `AggregateId`
```csharp
public string AggregateId { get; set; }
```
Gets or sets the identifier of the aggregate root to which this event belongs. This property links the event to a specific entity lifecycle within the domain.

### `AggregateType`
```csharp
public string AggregateType { get; set; }
```
Gets or sets the fully qualified name or logical type name of the aggregate root that produced this event. This is used during projection and replay to route events to the correct aggregate handler.

### `AggregateVersion`
```csharp
public long AggregateVersion { get; set; }
```
Gets or sets the version number of the aggregate immediately after this event was applied. This sequence number is critical for optimistic concurrency control and ensuring correct event ordering.

### `OccurredAt`
```csharp
public DateTime OccurredAt { get; set; }
```
Gets or sets the UTC timestamp indicating when the event occurred. This value is used for temporal queries and ordering events in time-based projections.

### `UserId`
```csharp
public string? UserId { get; set; }
```
Gets or sets the identifier of the user who triggered the action resulting in this event. This property is nullable and may be empty for system-generated events.

### `CorrelationId`
```csharp
public string? CorrelationId { get; set; }
```
Gets or sets the correlation ID used to trace a chain of related operations across different services or aggregates. This property is nullable and facilitates distributed tracing.

### `TenantId`
```csharp
public string? TenantId { get; set; }
```
Gets or sets the identifier of the tenant in a multi-tenant environment. This property is nullable and allows for data isolation logic during event handling.

### `Metadata`
```csharp
public Dictionary<string, object> Metadata { get; set; }
```
Gets or sets a dictionary containing arbitrary key-value pairs associated with the event. This collection allows for the attachment of contextual data not covered by standard properties without requiring schema changes.

### `GetEventType`
```csharp
public abstract string GetEventType();
```
Returns the specific type name of the event, typically used for serialization and deserialization routing. As an abstract member, derived classes must implement this to return a consistent string identifier representing the concrete event type.

### `PopulateMetadata`
```csharp
public virtual void PopulateMetadata();
```
Populates the `Metadata` dictionary with default or contextual values. Derived classes can override this method to inject specific data before the event is persisted. The base implementation may provide common defaults, but behavior depends on the specific override.

### `ToString`
```csharp
public override string ToString();
```
Returns a string representation of the event, typically including the event type, aggregate ID, and version for logging and debugging purposes.

## Usage

### Example 1: Creating and Initializing a Concrete Event
This example demonstrates deriving a concrete event from `DomainEvent` and initializing its core properties before persistence.

```csharp
public class OrderCreatedEvent : DomainEvent
{
    public string OrderNumber { get; init; }
    public decimal TotalAmount { get; init; }

    public override string GetEventType() => "OrderCreated";

    public override void PopulateMetadata()
    {
        base.PopulateMetadata();
        Metadata["OrderNumber"] = OrderNumber;
        Metadata["Source"] = "WebClient";
    }
}

// Usage context
var orderEvent = new OrderCreatedEvent
{
    EventId = Guid.NewGuid().ToString(),
    AggregateId = "ord_12345",
    AggregateType = "Order",
    AggregateVersion = 1,
    OccurredAt = DateTime.UtcNow,
    UserId = "user_987",
    CorrelationId = "corr_abcde",
    OrderNumber = "ORD-2023-001",
    TotalAmount = 199.99m
};

orderEvent.PopulateMetadata();
Console.WriteLine(orderEvent.ToString());
```

### Example 2: Inspecting Event Metadata and Context
This example shows how to access standard properties and custom metadata for auditing or projection logic.

```csharp
public void ProcessEvent(DomainEvent evt)
{
    // Access standard context
    if (!string.IsNullOrEmpty(evt.TenantId))
    {
        Console.WriteLine($"Processing for tenant: {evt.TenantId}");
    }

    // Access custom metadata safely
    if (evt.Metadata.TryGetValue("IpAddress", out var ipObj) && ipObj is string ip)
    {
        Console.WriteLine($"Event originated from IP: {ip}");
    }

    // Retrieve strong-typed event name
    var typeName = evt.GetEventType();
    Console.WriteLine($"Handling event type: {typeName} for Aggregate {evt.AggregateId} (v{evt.AggregateVersion})");
}
```

## Notes

*   **Thread Safety**: The `DomainEvent` class is not inherently thread-safe. Specifically, the `Metadata` property exposes a mutable `Dictionary<string, object>`. If an event instance is shared across multiple threads, external synchronization is required when reading or writing to the `Metadata` dictionary to prevent race conditions.
*   **Immutability Considerations**: While the core properties are defined with setters, in strict event sourcing implementations, these properties should typically be set once during construction and not modified afterward to preserve the integrity of the event log.
*   **Nullability**: Properties `UserId`, `CorrelationId`, and `TenantId` are nullable. Consumers must perform null checks before accessing these values to avoid `NullReferenceException`.
*   **Abstract Implementation**: Because `GetEventType` is abstract, any attempt to instantiate a class inheriting from `DomainEvent` without overriding this method will result in a compilation error.
*   **Metadata Overwrites**: When overriding `PopulateMetadata`, care should be taken to check for existing keys in the `Metadata` dictionary if the base implementation or previous calls have already populated data, as adding a duplicate key will throw an exception.
