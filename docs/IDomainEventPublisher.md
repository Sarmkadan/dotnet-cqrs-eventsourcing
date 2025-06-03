# IDomainEventPublisher

Provides a lightweight publish‑subscribe infrastructure for domain events within a CQRS/event‑sourced system. Implementations allow components to register interest in specific event types and to raise events asynchronously, decoupling producers from consumers.

## API

### DomainEventPublisher
**Purpose**  
Retrieves the underlying concrete publisher instance that this interface wraps.

**Parameters**  
None.

**Return Value**  
`DomainEventPublisher` – the concrete publisher object.

**Exceptions**  
- `ObjectDisposedException` if the publisher has been disposed.

### PublishAsync
**Purpose**  
Publishes a single domain event asynchronously to all currently subscribed handlers.

**Parameters**  
- `@event` – the domain event instance to publish (must not be `null`).

**Return Value**  
A `Task` that completes when all subscribed handlers have finished processing the event.

**Exceptions**  
- `ArgumentNullException` if `@event` is `null`.  
- `InvalidOperationException` if the publisher has been disposed.  
- Any exception thrown by a subscriber handler is propagated (aggregated if multiple handlers fail).

### PublishManyAsync
**Purpose**  
Publishes a collection of domain events asynchronously, preserving order.

**Parameters**  
- `events` – an `IEnumerable<IDomainEvent>` containing the events to publish; the enumeration is evaluated lazily.

**Return Value**  
A `Task` that completes when all events have been published and all handlers have finished.

**Exceptions**  
- `ArgumentNullException` if `events` is `null`.  
- `InvalidOperationException` if the publisher has been disposed.  
- Exceptions from individual handlers are propagated as with `PublishAsync`.

### Subscribe<T>
**Purpose**  
Registers a handler to be invoked when an event of type `T` is published.

**Parameters**  
- `handler` – an `Action<T>` that processes the event; must not be `null`.

**Return Value**  
None.

**Exceptions**  
- `ArgumentNullException` if `handler` is `null`.  
- `InvalidOperationException` if the publisher has been disposed.

### Unsubscribe<T>
**Purpose**  
Removes a previously registered handler for events of type `T`.

**Parameters**  
- `handler` – the `Action<T>` instance that was supplied to `Subscribe<T>`.

**Return Value**  
None.

**Exceptions**  
- `ArgumentNullException` if `handler` is `null`.  
- `InvalidOperationException` if the publisher has been disposed.  
- No exception is thrown if the handler was not currently subscribed.

### GetSubscriberCount<T>
**Purpose**  
Returns the number of handlers currently subscribed to events of type `T`.

**Parameters**  
None.

**Return Value**  
`int` – the count of subscribed handlers for `T`.

**Exceptions**  
- `InvalidOperationException` if the publisher has been disposed.

### Clear
**Purpose**  
Removes all subscriptions for all event types.

**Parameters**  
None.

**Return Value**  
None.

**Exceptions**  
- `InvalidOperationException` if the publisher has been disposed.

## Usage

### Subscribing and publishing a single event
```csharp
public class OrderCreatedHandler
{
    private readonly IDomainEventPublisher _publisher;

    public OrderCreatedHandler(IDomainEventPublisher publisher)
    {
        _publisher = publisher;
        // Subscribe to OrderCreated events
        _publisher.Subscribe<OrderCreated>(Handle);
    }

    private void Handle(OrderCreated @event)
    {
        // Update read model, send notifications, etc.
        Console.WriteLine($"Order { @event.OrderId } created.");
    }

    public void Dispose()
    {
        _publisher.Unsubscribe<OrderCreated>(Handle);
    }
}

// Somewhere in the application logic
var publisher = new DomainEventPublisher(); // concrete implementation
publisher.PublishAsync(new OrderCreated(Guid.NewGuid(), DateTime.UtcNow));
```

### Publishing many events and checking subscriber count
```csharp
var publisher = new DomainEventPublisher();

// Subscribe two handlers
publisher.Subscribe<ItemAdded>(h1);
publisher.Subscribe<ItemAdded>(h2);

Console.WriteLine(publisher.GetSubscriberCount<ItemAdded>()); // Output: 2

// Publish a batch of events
var batch = new List<IDomainEvent>
{
    new ItemAdded(Guid.NewGuid(), 1),
    new ItemAdded(Guid.NewGuid(), 2)
};
await publisher.PublishManyAsync(batch);

// Clear all subscriptions
publisher.Clear();
Console.WriteLine(publisher.GetSubscriberCount<ItemAdded>()); // Output: 0
```

## Notes
- Implementations should aim to make `Subscribe<T>`, `Unsubscribe<T>`, `GetSubscriberCount<T>`, and the publish methods thread‑safe; concurrent calls must not corrupt internal subscription lists.  
- The `Clear` method is **not** guaranteed to be thread‑safe with respect to ongoing publishes; calling `Clear` while a publish is in progress may result in undefined behavior.  
- Handlers are invoked synchronously on the thread that calls `PublishAsync`/`PublishManyAsync`; long‑running work should be off‑loaded to avoid blocking publishers.  
- If a subscriber throws an exception, the publishing method will propagate that exception after notifying all other subscribers (i.e., earlier subscribers still run).  
- The `DomainEventPublisher` property is intended for advanced scenarios where access to the concrete publisher is required (e.g., for testing or for integrating with external infrastructure); typical application code should interact solely through the `IDomainEventPublisher` interface.
