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
