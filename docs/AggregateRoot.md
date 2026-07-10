# AggregateRoot
The `AggregateRoot` type serves as a base class for domain models that require event sourcing and versioning, providing a set of common properties and methods for managing the aggregate's state and history. It is designed to be inherited by concrete aggregate root classes, which can then focus on their specific domain logic.

## API
* `public string Id`: Gets the unique identifier of the aggregate root.
* `public long Version`: Gets the current version of the aggregate root.
* `public DateTime CreatedAt`: Gets the date and time when the aggregate root was created.
* `public DateTime UpdatedAt`: Gets the date and time when the aggregate root was last updated.
* `public string? TenantId`: Gets the identifier of the tenant that owns the aggregate root, or `null` if not applicable.
* `public IReadOnlyList<DomainEvent> GetUncommittedEvents`: Gets a list of uncommitted domain events that have occurred since the last commit.
* `public void ClearUncommittedEvents()`: Clears the list of uncommitted domain events.
* `public void LoadFromHistory()`: Reconstructs the aggregate root's state from its event history.
* `public override string ToString()`: Returns a string representation of the aggregate root.

## Usage
The following examples demonstrate how to use the `AggregateRoot` class:
```csharp
public class Order : AggregateRoot
{
    public Order(string id, string customerId)
    {
        Id = id;
        // ...
    }

    public void PlaceOrder()
    {
        // ...
        var @event = new OrderPlacedEvent(Id, DateTime.UtcNow);
        // ...
    }
}

public class OrderService
{
    public void ProcessOrder(string orderId)
    {
        var order = new Order(orderId, "customer-123");
        order.LoadFromHistory();
        // ...
    }
}
```

## Notes
When using the `AggregateRoot` class, consider the following:
* The `LoadFromHistory` method may throw exceptions if the event history is corrupted or incomplete.
* The `ClearUncommittedEvents` method should be called after committing the aggregate root's changes to prevent event duplication.
* The `GetUncommittedEvents` property returns a snapshot of the uncommitted events at the time of access, and may not reflect subsequent changes.
* The `AggregateRoot` class is not thread-safe by default; implementers should consider synchronization mechanisms to ensure consistency in multi-threaded environments.
