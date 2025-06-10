# IProjectionWorker

A lightweight abstraction for managing the lifecycle of event-sourced projections, allowing callers to rebuild, pause, or resume projection processing in a controlled manner.

## API

### `ProjectionWorker`

The default implementation of `IProjectionWorker` provided by the library. It is designed to be instantiated via dependency injection and supports asynchronous operation throughout its lifetime.

### `async Task RebuildProjectionAsync()`

Initiates a full rebuild of the projection by replaying all relevant events from the event store. This operation is idempotent; if a rebuild is already in progress, subsequent calls will complete immediately without error.

- **Returns**: A `Task` that completes when the rebuild operation has finished.
- **Throws**:
  - `InvalidOperationException`: If the projection is currently paused.
  - `OperationCanceledException`: If the rebuild is canceled via the linked cancellation token.

### `async Task PauseAsync()`

Temporarily halts the processing of new events for the projection. Existing in-flight events may still be processed depending on implementation, but no new events will be dispatched until `ResumeAsync` is called. This method is safe to call multiple times; only the first invocation has effect.

- **Returns**: A `Task` that completes when the projection has been paused.
- **Throws**:
  - `InvalidOperationException`: If the projection is already paused or currently rebuilding.

### `async Task ResumeAsync()`

Re-enables event processing for the projection after a prior call to `PauseAsync`. Events that arrived during the paused state will be processed in order once resumed. This method is idempotent; repeated calls have no additional effect.

- **Returns**: A `Task` that completes when the projection has resumed normal operation.
- **Throws**:
  - `InvalidOperationException`: If the projection is currently rebuilding.

## Usage

Rebuilding a projection after schema changes:
