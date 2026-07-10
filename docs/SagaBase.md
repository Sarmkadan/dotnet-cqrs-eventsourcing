# SagaBase
The `SagaBase` type serves as a foundation for implementing sagas in a CQRS (Command Query Responsibility Segregation) and event sourcing architecture. It provides a set of common properties and methods that can be used to manage the state and behavior of a saga, which is a series of events that are executed as a single, all-or-nothing unit of work.

## API
* `public string SagaId`: A unique identifier for the saga instance.
* `public abstract string SagaName`: The name of the saga, which must be implemented by derived classes.
* `public SagaState State`: The current state of the saga.
* `public DateTime StartedAt`: The date and time when the saga was started.
* `public DateTime? LastUpdatedAt`: The date and time when the saga was last updated, or null if it has not been updated.
* `public string? CorrelationId`: An optional correlation identifier for the saga.
* `public void ClearOutboxEvents()`: Clears any pending outbox events for the saga.
* `public override string ToString()`: Returns a string representation of the saga instance.

## Usage
The following examples demonstrate how to use the `SagaBase` type:
```csharp
public class MySaga : SagaBase
{
    public MySaga(string id) : base()
    {
        SagaId = id;
    }

    public override string SagaName => "MySaga";
}

// Create a new instance of the saga
var saga = new MySaga("123");
Console.WriteLine(saga.ToString());

// Clear any pending outbox events
saga.ClearOutboxEvents();
```

## Notes
When using the `SagaBase` type, note that the `SagaName` property must be implemented by derived classes. Additionally, the `ClearOutboxEvents` method will only clear events that are pending in the outbox, and will not affect events that have already been published. The `SagaBase` type is not thread-safe by default, and care should be taken to ensure that instances are properly synchronized if accessed from multiple threads. The `StartedAt` and `LastUpdatedAt` properties are automatically set when the saga is started and updated, respectively. The `CorrelationId` property can be used to correlate the saga with external events or processes.
