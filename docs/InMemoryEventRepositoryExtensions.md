# InMemoryEventRepositoryExtensions

The `InMemoryEventRepositoryExtensions` class provides a set of static asynchronous extension methods designed to simplify common query operations against an in-memory event repository implementation. These utilities facilitate efficient retrieval of specific event envelopes, existence checks, and count aggregations without requiring direct manipulation of the underlying repository collection, returning standardized `Result` wrappers to handle success and failure states consistently.

## API

### GetFirstEventAsync
Retrieves the earliest event envelope associated with a specific aggregate identifier.
*   **Parameters**: Accepts the target `IInMemoryEventRepository` instance and the `aggregateId` of the stream to query.
*   **Return Value**: Returns a `Task<Result<EventEnvelope>>`. On success, the result contains the first `EventEnvelope` in the stream; on failure (e.g., stream not found or empty), the result indicates an error.
*   **Exceptions**: Throws if the repository instance is null or if the underlying in-memory storage encounters an unexpected state corruption.

### GetLastEventAsync
Retrieves the most recent event envelope associated with a specific aggregate identifier.
*   **Parameters**: Accepts the target `IInMemoryEventRepository` instance and the `aggregateId` of the stream to query.
*   **Return Value**: Returns a `Task<Result<EventEnvelope>>`. On success, the result contains the last `EventEnvelope` appended to the stream; on failure, the result indicates an error.
*   **Exceptions**: Throws if the repository instance is null or if the underlying in-memory storage encounters an unexpected state corruption.

### AggregateExistsAsync
Determines whether an event stream exists for a given aggregate identifier.
*   **Parameters**: Accepts the target `IInMemoryEventRepository` instance and the `aggregateId` to check.
*   **Return Value**: Returns a `Task<Result<bool>>`. The boolean value is `true` if at least one event exists for the aggregate, otherwise `false`.
*   **Exceptions**: Throws if the repository instance is null.

### GetEventCountAsync
Calculates the total number of events currently stored in a specific aggregate stream.
*   **Parameters**: Accepts the target `IInMemoryEventRepository` instance and the `aggregateId` of the stream to count.
*   **Return Value**: Returns a `Task<Result<int>>`. The integer represents the total count of events in the stream.
*   **Exceptions**: Throws if the repository instance is null.

## Usage

The following example demonstrates checking for aggregate existence before retrieving the latest event to determine the current state version.

```csharp
using Cqrs.Eventsourcing;
using Cqrs.Eventsourcing.InMemory;

public async Task ProcessLatestEventAsync(IInMemoryEventRepository repository, string aggregateId)
{
    var existsResult = await repository.AggregateExistsAsync(aggregateId);
    
    if (!existsResult.IsSuccess || !existsResult.Value)
    {
        // Handle case where aggregate has no history
        return;
    }

    var lastEventResult = await repository.GetLastEventAsync(aggregateId);
    
    if (lastEventResult.IsSuccess)
    {
        var envelope = lastEventResult.Value;
        Console.WriteLine($"Latest event type: {envelope.EventType} at version {envelope.Version}");
    }
}
```

The following example illustrates retrieving the initial event in a stream and counting the total events for audit logging purposes.

```csharp
using Cqrs.Eventsourcing;
using Cqrs.Eventsourcing.InMemory;

public async Task AuditStreamAsync(IInMemoryEventRepository repository, string aggregateId)
{
    var firstEventResult = await repository.GetFirstEventAsync(aggregateId);
    var countResult = await repository.GetEventCountAsync(aggregateId);

    if (firstEventResult.IsSuccess && countResult.IsSuccess)
    {
        var originEvent = firstEventResult.Value;
        var totalEvents = countResult.Value;

        Console.WriteLine($"Stream started with {originEvent.EventType}. Total events: {totalEvents}");
    }
    else
    {
        // Handle retrieval errors
        Console.WriteLine("Failed to retrieve stream metadata.");
    }
}
```

## Notes

*   **Thread Safety**: As this class operates on an in-memory repository, concurrent access to the same `aggregateId` from multiple threads may lead to race conditions if the underlying `IInMemoryEventRepository` implementation does not enforce internal synchronization. Callers should ensure external locking or utilize repository implementations that guarantee thread safety for read operations.
*   **Empty Streams**: Invoking `GetFirstEventAsync` or `GetLastEventAsync` on an aggregate ID that has no associated events will result in a failed `Result`, rather than returning null. Consumers must check `IsSuccess` before accessing the `Value` property.
*   **Consistency**: The `GetEventCountAsync` method reflects the state of the in-memory collection at the exact moment of execution. In highly concurrent environments where events are being appended simultaneously, the count may become stale immediately after retrieval.
*   **Null Arguments**: Passing a `null` repository instance or a `null`/empty `aggregateId` (depending on repository validation rules) will typically result in an exception being thrown rather than a failed `Result`, as these represent invalid usage of the extension method itself.
