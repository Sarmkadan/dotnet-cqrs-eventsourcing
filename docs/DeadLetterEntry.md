# DeadLetterEntry

Represents a domain event that failed to be processed by a projection and was moved to a dead letter queue for later inspection or reprocessing. Each entry captures the original event, the projection that failed, the error details, and the retry history.

## API

### `Id` (string)
Unique identifier for this dead letter entry. Typically a GUID or a deterministic key derived from the event and projection name. Read-only after construction.

### `Event` (DomainEvent)
The original domain event that caused the failure. This property is set at creation and should not be null.

### `ProjectionName` (string)
The name of the projection that was processing the event when the failure occurred. Used to route the entry back to the correct handler during reprocessing.

### `ErrorMessage` (string)
The exception message or a textual description of the failure reason. May be empty if no message was captured.

### `AttemptCount` (int)
Number of times the event has been attempted for this projection. Incremented each time the event is retried. Starts at 1 for the initial failure.

### `FailedAt` (DateTime)
UTC timestamp indicating when the failure first occurred (or when the entry was created).

### `IsReprocessed` (bool)
Indicates whether this entry has been successfully reprocessed. Initially `false`. Set to `true` by `MarkReprocessed`.

### `ReprocessedAt` (DateTime?)
UTC timestamp of when the entry was reprocessed. `null` until `MarkReprocessed` is called.

### `MarkReprocessed()`
Marks the entry as reprocessed by setting `IsReprocessed` to `true` and `ReprocessedAt` to the current UTC time (`DateTime.UtcNow`).  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** This method does not throw. It is safe to call multiple times; subsequent calls will overwrite `ReprocessedAt` with the latest timestamp.

## Usage

### Example 1: Inspecting a dead letter entry

```csharp
var entry = new DeadLetterEntry
{
    Id = Guid.NewGuid().ToString(),
    Event = new OrderShippedEvent(orderId: "ORD-123"),
    ProjectionName = "OrderSummaryProjection",
    ErrorMessage = "Null reference on customer address",
    AttemptCount = 3,
    FailedAt = DateTime.UtcNow,
    IsReprocessed = false,
    ReprocessedAt = null
};

Console.WriteLine($"Entry {entry.Id} failed on projection {entry.ProjectionName} after {entry.AttemptCount} attempts.");
if (!entry.IsReprocessed)
{
    Console.WriteLine("Not yet reprocessed.");
}
```

### Example 2: Reprocessing a failed entry

```csharp
DeadLetterEntry entry = await deadLetterStore.GetByIdAsync("abc-123");

// Attempt to reprocess the event
try
{
    await projectionHandler.HandleAsync(entry.Event);
    entry.MarkReprocessed();
    await deadLetterStore.UpdateAsync(entry);
    Console.WriteLine($"Entry {entry.Id} reprocessed at {entry.ReprocessedAt}.");
}
catch (Exception ex)
{
    entry.AttemptCount++;
    entry.ErrorMessage = ex.Message;
    await deadLetterStore.UpdateAsync(entry);
    Console.WriteLine($"Reprocessing failed again. Attempt count now {entry.AttemptCount}.");
}
```

## Notes

- **Nullability:** `Event` should never be null; `ReprocessedAt` is null until `MarkReprocessed` is called. `ErrorMessage` may be an empty string.
- **Id uniqueness:** The `Id` is expected to be unique across all dead letter entries. Duplicate IDs may cause store-level conflicts.
- **Thread safety:** This type is not thread-safe. Concurrent reads and writes to the same instance should be synchronized externally (e.g., via locks or by using immutable copies).
- **Multiple calls to `MarkReprocessed`:** Calling the method more than once will update `ReprocessedAt` to the latest timestamp. This is by design to allow re‑reprocessing if the first reprocessing was later invalidated.
- **AttemptCount semantics:** The initial failure sets `AttemptCount` to 1. Each subsequent retry (including failed reprocessing attempts) should increment this value manually, as shown in Example 2.
