# InMemoryDeadLetterStore
The `InMemoryDeadLetterStore` class is designed to store and manage dead letter events in memory, providing a simple and efficient way to handle events that cannot be processed by an event handler. It allows for writing, retrieving, and marking events as reprocessed, making it a useful tool for event sourcing and CQRS architectures.

## API
* `public InMemoryDeadLetterStore`: The constructor for the `InMemoryDeadLetterStore` class, used to create a new instance.
* `public Task WriteAsync`: Writes a dead letter event to the store. Parameters and return values are not specified, but it is expected to throw an exception if the write operation fails.
* `public Task<IReadOnlyList<DeadLetterEntry>> GetByProjectionAsync`: Retrieves a list of dead letter events associated with a specific projection. The method returns a task that resolves to a read-only list of `DeadLetterEntry` objects. It may throw an exception if the retrieval operation fails.
* `public Task<IReadOnlyList<DeadLetterEntry>> GetByAggregateAsync`: Retrieves a list of dead letter events associated with a specific aggregate. The method returns a task that resolves to a read-only list of `DeadLetterEntry` objects. It may throw an exception if the retrieval operation fails.
* `public Task<IReadOnlyList<DeadLetterEntry>> GetAllAsync`: Retrieves all dead letter events in the store. The method returns a task that resolves to a read-only list of `DeadLetterEntry` objects. It may throw an exception if the retrieval operation fails.
* `public Task<Result> MarkReprocessedAsync`: Marks a dead letter event as reprocessed. The method returns a task that resolves to a `Result` object, indicating the outcome of the operation. It may throw an exception if the mark operation fails.
* `public Task<int> GetCountAsync`: Retrieves the total count of dead letter events in the store. The method returns a task that resolves to an integer representing the count. It may throw an exception if the retrieval operation fails.

## Usage
```csharp
// Example 1: Writing and retrieving dead letter events
var deadLetterStore = new InMemoryDeadLetterStore();
await deadLetterStore.WriteAsync(); // Write a dead letter event
var events = await deadLetterStore.GetAllAsync(); // Retrieve all dead letter events
Console.WriteLine($"Retrieved {events.Count} dead letter events");
```

```csharp
// Example 2: Marking a dead letter event as reprocessed
var deadLetterStore = new InMemoryDeadLetterStore();
await deadLetterStore.WriteAsync(); // Write a dead letter event
var result = await deadLetterStore.MarkReprocessedAsync(); // Mark the event as reprocessed
if (result.IsSuccess)
{
    Console.WriteLine("Dead letter event marked as reprocessed successfully");
}
else
{
    Console.WriteLine("Failed to mark dead letter event as reprocessed");
}
```

## Notes
The `InMemoryDeadLetterStore` class stores events in memory, which means that all events will be lost when the application restarts. This makes it suitable for development and testing environments, but not for production environments where event persistence is required. Additionally, the class is not designed to be thread-safe, so it should not be used in concurrent scenarios without proper synchronization. The `GetByProjectionAsync` and `GetByAggregateAsync` methods may return an empty list if no events are associated with the specified projection or aggregate. The `MarkReprocessedAsync` method may return a failed result if the event is not found or if the mark operation fails.
