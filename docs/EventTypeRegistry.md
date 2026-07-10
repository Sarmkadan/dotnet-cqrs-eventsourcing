# EventTypeRegistry

The `EventTypeRegistry` acts as a central repository for mapping stable event-name strings—typically defined via an `[EventName]` attribute—to their concrete `DomainEvent` types. This mechanism provides a robust alternative to using `Type.GetType()` for deserialization, ensuring that event handlers and stores remain resilient even when assemblies are renamed or namespaces are refactored.

## API

### `EventTypeRegistry(ILogger<EventTypeRegistry>? logger = null)`
Initializes a new instance of the registry.
*   **Parameters:**
    *   `logger`: Optional logger instance to record registration events.

### `void Register<T>(string eventName)`
Registers a concrete domain event type `T` with a stable identifier.
*   **Parameters:**
    *   `T`: The concrete `DomainEvent` type to register.
    *   `eventName`: The unique, stable string identifier for the event.
*   **Exceptions:**
    *   `ArgumentException`: Thrown if `eventName` is null or whitespace.
    *   `InvalidOperationException`: Thrown if `eventName` is already registered to a different type.

### `void ScanAssembly(Assembly assembly)`
Scans the provided assembly for types inheriting from `DomainEvent` that are decorated with an `EventNameAttribute` and registers them.
*   **Parameters:**
    *   `assembly`: The assembly to scan.

### `Type? Resolve(string eventName)`
Looks up the `Type` registered under the given `eventName`.
*   **Parameters:**
    *   `eventName`: The stable event identifier to look up.
*   **Returns:** The `Type` if found; otherwise `null`.

### `bool TryResolve(string eventName, out Type? type)`
Attempts to look up the `Type` registered under the given `eventName`.
*   **Parameters:**
    *   `eventName`: The stable event identifier to look up.
    *   `type`: When the method returns, contains the `Type` if found, or `null` if not found.
*   **Returns:** `true` if the event name is registered; otherwise `false`.

### `IReadOnlyDictionary<string, Type> GetAllRegistrations()`
Returns a snapshot of all current event-name to type mappings.
*   **Returns:** An `IReadOnlyDictionary` containing the registration map.

## Usage

### Manual Registration
```csharp
var registry = new EventTypeRegistry();
registry.Register<AccountCreatedEvent>("AccountCreated");

if (registry.TryResolve("AccountCreated", out var eventType))
{
    // Use eventType for deserialization
}
```

### Automatic Assembly Scanning
```csharp
var registry = new EventTypeRegistry();
// Scans all classes in the domain assembly decorated with [EventName]
registry.ScanAssembly(typeof(DomainEvent).Assembly);

var allEvents = registry.GetAllRegistrations();
foreach (var mapping in allEvents)
{
    Console.WriteLine($"Name: {mapping.Key}, Type: {mapping.Value.Name}");
}
```

## Notes

*   **Thread-Safety:** This registry is thread-safe. It utilizes a `ConcurrentDictionary` internally to manage registrations, allowing concurrent access and modification without external locking.
*   **Uniqueness:** Event names must be unique within a single `EventTypeRegistry` instance. Attempting to register an existing event name to a different type will result in an `InvalidOperationException`.
*   **Resolution:** The lookup is case-sensitive, following `StringComparer.Ordinal` behavior by default. `Resolve` and `TryResolve` return `null` or `false` respectively when an unknown `eventName` is provided, rather than throwing an exception.
