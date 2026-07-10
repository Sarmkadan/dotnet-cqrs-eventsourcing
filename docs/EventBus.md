# EventBus

A lightweight in-memory event dispatcher that allows components to publish events and subscribe/unsubscribe to specific event types without direct coupling. It supports both single-event and batch publishing, with optional persistence hooks for event-sourcing scenarios.

## API

### `EventBus`

The core class that manages event subscriptions and dispatching.

### `public Task<Result> PublishEventAsync<TEvent>(TEvent @event)`

Publishes a single event of type `TEvent` to all current subscribers.

- **Parameters**
  - `@event`: The event instance to publish.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `@event` is `null`.

### `public async Task<Result> PublishEventsAsync<TEvent>(IEnumerable<TEvent> events)`

Publishes a sequence of events of type `TEvent` to all current subscribers.

- **Parameters**
  - `events`: A collection of event instances to publish.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `events` is `null`.
  - Throws `ArgumentException` if any item in `events` is `null`.

### `public void Subscribe<TEvent>(Action<TEvent> handler)`

Registers a synchronous handler for events of type `TEvent`.

- **Parameters**
  - `handler`: The delegate to invoke when an event of type `TEvent` is published.
- **Exceptions**
  - Throws `ArgumentNullException` if `handler` is `null`.

### `public void Unsubscribe<TEvent>(Action<TEvent> handler)`

Removes a previously registered synchronous handler for events of type `TEvent`.

- **Parameters**
  - `handler`: The delegate to remove.
- **Exceptions**
  - Throws `ArgumentNullException` if `handler` is `null`.

### `public async Task<Result> PublishAndPersistAsync<TEvent>(TEvent @event)`

Publishes a single event and triggers persistence logic, typically used in event-sourced systems.

- **Parameters**
  - `@event`: The event instance to publish and persist.
- **Return value**
  - A `Task<Result>` indicating success or failure of the operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `@event` is `null`.

## Usage

### Subscribing and Publishing
