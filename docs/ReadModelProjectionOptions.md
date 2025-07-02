# ReadModelProjectionOptions

The `ReadModelProjectionOptions` class provides configuration settings for read model projection processing in a CQRS/ES system. It controls retry behaviour, checkpointing, concurrency, timeouts, and dead letter handling. The class also exposes two nested sealed record types used to represent projection checkpoints and rebuild results.

## API

### `MaxRetryAttempts` (int)

Gets or sets the maximum number of retry attempts for a failed projection operation.  
The getter returns the current value. The setter may throw an `ArgumentOutOfRangeException` if the value is less than zero.

### `RetryBaseDelayMilliseconds` (int)

Gets or sets the base delay in milliseconds between retry attempts.  
The getter returns the current value. The setter may throw an `ArgumentOutOfRangeException` if the value is less than zero.

### `EnableCheckpointing` (bool)

Gets or sets a value indicating whether checkpointing of projection progress is enabled. When `true`, the projection engine periodically saves the current position.

### `CheckpointInterval` (int)

Gets or sets the number of events processed between checkpoints. This value is only relevant when `EnableCheckpointing` is `true`.  
The getter returns the current value. The setter may throw an `ArgumentOutOfRangeException` if the value is less than or equal to zero.

### `MaxConcurrentProjectors` (int)

Gets or sets the maximum number of projector instances that can run concurrently.  
The getter returns the current value. The setter may throw an `ArgumentOutOfRangeException` if the value is less than one.

### `ProjectorTimeout` (TimeSpan)

Gets or sets the timeout duration for a single projector operation. If the operation exceeds this duration, it is considered failed.  
The getter returns the current value. The setter may throw an `ArgumentOutOfRangeException` if the value is less than or equal to `TimeSpan.Zero`.

### `ClearCheckpointsBeforeRebuild` (bool)

Gets or sets a value indicating whether all existing checkpoints are cleared before initiating a projection rebuild.

### `EnableDeadLetterStore` (bool)

Gets or sets a value indicating whether the dead letter store is enabled for failed projection events.

### `ProjectionCheckpoint` (sealed record)

A nested sealed record type that represents a checkpoint entry. Instances of this type are used to track the projection’s position in the event stream. The record provides value equality and is immutable.

### `ProjectionRebuildResult` (sealed record)

A nested sealed record type that represents the result of a projection rebuild operation. It contains status information and, if applicable, error details. The record provides value equality and is immutable.

## Usage

The following example demonstrates how to configure a `ReadModelProjectionOptions` instance for a typical projection pipeline.

```csharp
var options = new ReadModelProjectionOptions
{
    MaxRetryAttempts = 3,
    RetryBaseDelayMilliseconds = 1000,
    EnableCheckpointing = true,
    CheckpointInterval = 100,
    MaxConcurrentProjectors = 2,
    ProjectorTimeout = TimeSpan.FromMinutes(5),
    ClearCheckpointsBeforeRebuild = true,
    EnableDeadLetterStore = true
};

// Pass options to a projection host or engine
var host = new ProjectionHost(options);
await host.StartAsync();
```

The next example illustrates how the nested record types might be used within a projection handler.

```csharp
public class OrderProjectionHandler
{
    public async Task HandleAsync(
        IReadOnlyList<Event> events,
        ReadModelProjectionOptions.ProjectionCheckpoint currentCheckpoint,
        CancellationToken cancellationToken)
    {
        // currentCheckpoint provides the last processed position
        // Process events and update the read model

        // After processing, create a new checkpoint
        var newCheckpoint = new ReadModelProjectionOptions.ProjectionCheckpoint(/* ... */);
        // Save checkpoint via the projection engine
    }

    public async Task<ReadModelProjectionOptions.ProjectionRebuildResult> RebuildAsync(
        ReadModelProjectionOptions options,
        CancellationToken cancellationToken)
    {
        // Perform full rebuild
        return new ReadModelProjectionOptions.ProjectionRebuildResult(/* ... */);
    }
}
```

## Notes

- **Edge cases**:  
  - Setting `MaxRetryAttempts` to `0` disables retries entirely.  
  - A `RetryBaseDelayMilliseconds` of `0` causes immediate retries with no delay.  
  - `CheckpointInterval` must be a positive integer when `EnableCheckpointing` is `true`; otherwise the projection engine may throw an exception at runtime.  
  - `ProjectorTimeout` must be a positive `TimeSpan`; a zero or negative value will cause an `ArgumentOutOfRangeException` on assignment.

- **Thread safety**:  
  The instance members of `ReadModelProjectionOptions` are not thread-safe for concurrent writes. It is recommended to configure the options before starting any projection processing and treat the instance as immutable thereafter. The nested `ProjectionCheckpoint` and `ProjectionRebuildResult` records are immutable and inherently thread-safe.
