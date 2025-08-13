// existing content ...

## EventTypeRegistry

The `EventTypeRegistry` is a utility class that helps manage event types in your application. It allows you to register event types, scan assemblies for event types, and resolve event types by name.

### Usage Example

```csharp
var registry = new EventTypeRegistry();

// Register an event type
registry.Register<MyEvent>();

// Scan an assembly for event types
registry.ScanAssembly(typeof(MyAssembly).Assembly);

// Resolve an event type by name
var eventType = registry.Resolve("MyEvent");
if (eventType != null)
{
    Console.WriteLine("Event type found.");
}

// Get all registered event types
var registrations = registry.GetAllRegistrations();
foreach (var registration in registrations)
{
    Console.WriteLine($"Event type: {registration.Key}");
}
```

## IDomainEventPublisher

The `IDomainEventPublisher` interface defines a contract for publishing domain events in your application. It provides methods for publishing individual events, publishing multiple events, subscribing to events, and clearing event subscriptions.

### Usage Example

```csharp
var publisher = new DomainEventPublisher();

// Subscribe to a specific event type
publisher.Subscribe<MyEvent>();

// Publish an event
await publisher.PublishAsync(new MyEvent());

// Publish multiple events
await publisher.PublishManyAsync(new[] { new MyEvent1(), new MyEvent2() });

// Get the number of subscribers for a specific event type
var subscriberCount = publisher.GetSubscriberCount<MyEvent>();

// Clear all event subscriptions
publisher.Clear();
```
```