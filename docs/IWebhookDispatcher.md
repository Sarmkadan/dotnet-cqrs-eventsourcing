# IWebhookDispatcher

The `IWebhookDispatcher` interface defines the contract for managing and executing HTTP webhook notifications within the `dotnet-cqrs-eventsourcing` system. It enables the registration of event handlers that are triggered asynchronously when specific event types occur, facilitating decoupled integration with external services. The dispatcher handles the lifecycle of webhook subscriptions, including registration, unregistration, and the invocation of registered endpoints when corresponding events are dispatched.

## API

### Constructor
*   `WebhookDispatcher()`
    Initializes a new instance of the `WebhookDispatcher` class.

### Methods

*   `Task RegisterWebhookAsync(WebhookRegistration registration)`
    Registers a new webhook.
    *   **Parameters:** `registration` (the `WebhookRegistration` configuration object).
    *   **Returns:** A task representing the asynchronous registration operation.

*   `Task UnregisterWebhookAsync(Guid webhookId)`
    Removes an existing webhook registration.
    *   **Parameters:** `webhookId` (the unique identifier of the webhook to remove).
    *   **Returns:** A task representing the asynchronous unregistration operation.

*   `async Task DispatchAsync<TEvent>(TEvent @event)`
    Invokes all active webhook endpoints registered for the specified event type.
    *   **Parameters:** `@event` (the event instance to dispatch).
    *   **Returns:** A task representing the asynchronous dispatch operation.

*   `Task<IEnumerable<WebhookRegistration>> GetRegistrationsAsync()`
    Retrieves all currently stored webhook registrations.
    *   **Returns:** A task resulting in an enumerable collection of `WebhookRegistration` objects.

### Properties

*   `Guid Id`
    The unique identifier for the webhook registration.

*   `string WebhookUrl`
    The destination URL for the webhook notification.

*   `Type EventType`
    The type of event that triggers this webhook.

*   `DateTime RegisteredAt`
    The timestamp indicating when the webhook was registered.

*   `bool Active`
    Indicates whether the webhook is currently enabled and eligible for dispatch.

## Usage

### Registering and Dispatching a Webhook

```csharp
var dispatcher = new WebhookDispatcher();

var registration = new WebhookRegistration
{
    Id = Guid.NewGuid(),
    WebhookUrl = "https://external-service.com/webhook",
    EventType = typeof(OrderPlacedEvent),
    RegisteredAt = DateTime.UtcNow,
    Active = true
};

await dispatcher.RegisterWebhookAsync(registration);

// Triggered by the system when an event occurs
await dispatcher.DispatchAsync(new OrderPlacedEvent { OrderId = 123 });
```

### Retrieving and Managing Registrations

```csharp
var dispatcher = new WebhookDispatcher();
var registrations = await dispatcher.GetRegistrationsAsync();

foreach (var reg in registrations)
{
    if (!reg.Active)
    {
        await dispatcher.UnregisterWebhookAsync(reg.Id);
    }
}
```

## Notes

*   **Thread Safety:** Implementations of `IWebhookDispatcher` are expected to be thread-safe regarding registration management. Concurrent calls to `RegisterWebhookAsync` and `UnregisterWebhookAsync` should be handled using appropriate locking mechanisms to ensure the integrity of the underlying registration store.
*   **Dispatch Behavior:** `DispatchAsync` is an asynchronous operation. If a webhook endpoint is unresponsive or returns a non-success status code, the dispatcher may log the failure, but it should not prevent the dispatching of other registered webhooks.
*   **Error Handling:** Registrations should be validated before storage. `RegisterWebhookAsync` may throw `ArgumentException` if the provided URL is malformed or if the registration object is incomplete.
