// existing content ...

## ISnapshotCompressionService

The `ISnapshotCompressionService` interface provides a way to compress and decompress aggregate snapshots. It allows for compressing a snapshot into a compressed format, decompressing a compressed snapshot back into its original format, and retrieving statistics about the compression process.

### Usage Example

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var service = new SnapshotCompressionService();
        var originalSnapshot = new AggregateSnapshot(); // Assume this is a valid aggregate snapshot
        var compressedSnapshot = await service.CompressAsync(originalSnapshot);
        Console.WriteLine($"Compressed snapshot: {compressedSnapshot}");

        var decompressedSnapshot = await service.DecompressAsync(compressedSnapshot);
        Console.WriteLine($"Decompressed snapshot: {decompressedSnapshot}");

        var stats = service.GetStats();
        Console.WriteLine($"Snapshots processed: {stats.SnapshotsProcessed}");
        Console.WriteLine($"Total original bytes: {stats.TotalOriginalBytes}");
        Console.WriteLine($"Total compressed bytes: {stats.TotalCompressedBytes}");
        Console.WriteLine($"Overall compression ratio: {stats.OverallCompressionRatio}");
    }
}
```

## IHttpClientFactory

The `IHttpClientFactory` interface provides a way to create instances of `HttpClient` with various configurations. It allows for the creation of clients with base addresses, authentication, and other settings.

### Usage Example

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new StandardHttpClientFactory();
        var client = factory.CreateClient("MyClient");
        client.BaseAddress = new Uri("https://example.com/api");

        var authenticatedClient = factory.CreateAuthenticatedClient("MyAuthenticatedClient");
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "my_token");

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddStandardHttpClients();
        serviceCollection.AddHttpClientResilience();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var clientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("MyClient");
    }
}
```

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

## IWebhookDispatcher

The `IWebhookDispatcher` is responsible for managing webhook registrations and dispatching events to registered webhook URLs. It allows you to register and unregister webhooks, dispatch events to all active registrations, and query the current registrations.

### Usage Example

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;

// Create a dispatcher instance
var dispatcher = new WebhookDispatcher();

// Register a webhook for a specific event type
await dispatcher.RegisterWebhookAsync(
    "https://example.com/webhook",
    typeof(MyEvent));

// Dispatch an event to all active webhooks
await dispatcher.DispatchAsync(new MyEvent());

// Retrieve all current registrations
var registrations = await dispatcher.GetRegistrationsAsync();

foreach (var reg in registrations)
{
    Console.WriteLine(
        $"Webhook {reg.Id} ({reg.WebhookUrl}) for event {reg.EventType.Name} " +
        $"registered at {reg.RegisteredAt}, active: {reg.Active}");
}

// Unregister a webhook by its Id
if (registrations.Any())
{
    await dispatcher.UnregisterWebhookAsync(registrations.First().Id);
}
```

The dispatcher exposes the following public members:

- `RegisterWebhookAsync(string webhookUrl, Type eventType)` – Registers a new webhook.
- `UnregisterWebhookAsync(Guid registrationId)` – Removes an existing webhook.
- `DispatchAsync(object @event)` – Sends the event to all active webhooks.
- `GetRegistrationsAsync()` – Returns all webhook registrations.
- `WebhookRegistration` properties: `Id`, `WebhookUrl`, `EventType`, `RegisteredAt`, `Active`.

These members provide a straightforward API for integrating webhook-based event notifications into your CQRS and event‑sourcing architecture.
```