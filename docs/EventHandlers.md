# EventHandlers

The `EventHandlers` class serves as the foundational abstraction for defining event processing logic within the `dotnet-cqrs-eventsourcing` framework. It encapsulates the lifecycle of handling domain events, providing a structured approach to registering specific handlers, executing asynchronous processing logic, managing error states, and coordinating complex transactional steps including compensation strategies for failed operations. As an abstract base, it enforces a consistent contract for event consumption while allowing concrete implementations to define specific business rules and persistence mechanisms.

## API

### `public EventHandlers`
The public constructor initializes a new instance of the `EventHandlers` class. It prepares the internal state required for handler registration and lifecycle management. No parameters are accepted, and it does not return a value. This constructor does not throw exceptions under normal initialization conditions.

### `public void RegisterHandlers`
Registers the specific event handling delegates or logic associated with this instance. This method configures the internal mapping between event types and their corresponding processing routines. It accepts no parameters and returns `void`. Exceptions may be thrown if the registration process encounters invalid configurations, such as duplicate handlers for the same event type or null references within the registration logic.

### `public abstract Task HandleAsync`
Executes the primary asynchronous logic for processing a received event. As an abstract member, concrete implementations must define the specific behavior for interpreting and acting upon event data. It returns a `Task` that completes when the handling logic finishes successfully. Implementations may throw exceptions if the event payload is malformed, business rules are violated, or underlying infrastructure resources are unavailable.

### `public virtual Task HandleErrorAsync`
Provides a hook for handling exceptions that occur during the event processing lifecycle. The default implementation is virtual, allowing derived classes to override the error handling strategy (e.g., logging, retrying, or moving to a dead-letter queue). It returns a `Task` representing the asynchronous error handling operation. This method typically receives context regarding the failure (depending on implementation specifics) and should not re-throw the original exception unless the error is unrecoverable.

### `public abstract Task StartAsync`
Initiates the asynchronous workflow or transaction scope required before the main event handling logic proceeds. This abstract method forces implementations to define initialization steps, such as acquiring locks, opening database transactions, or validating preconditions. It returns a `Task` that completes when the startup phase is finished. Exceptions may be thrown if the system is not in a valid state to begin processing or if resource allocation fails.

### `public abstract Task CompleteStepAsync`
Finalizes a specific step within the event processing workflow. This abstract method is invoked to commit changes or advance the state machine after a successful operation segment. It returns a `Task` indicating completion. Failures here typically indicate issues with persisting state or committing transactions, resulting in thrown exceptions that trigger the compensation flow.

### `public abstract Task CompensateAsync`
Executes compensation logic to revert changes or undo side effects when a step in the processing workflow fails. This abstract method is critical for maintaining data consistency in distributed or transactional scenarios. It returns a `Task` that completes when the rollback or compensation actions are finished. Exceptions may be thrown if the compensation itself fails, potentially requiring manual intervention or further escalation.

## Usage

### Example 1: Basic Event Handling Implementation
This example demonstrates a concrete implementation of `EventHandlers` that processes a `UserRegisteredEvent`. It overrides the required abstract methods to define the processing flow.

```csharp
public class UserRegistrationHandlers : EventHandlers
{
    private readonly IUserRepository _userRepository;

    public UserRegistrationHandlers(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        RegisterHandlers();
    }

    public override void RegisterHandlers()
    {
        // Logic to map UserRegisteredEvent to HandleAsync would be configured here
        // depending on the internal dispatcher mechanism of the base class.
    }

    public override async Task StartAsync()
    {
        // Prepare transaction or load aggregate root
        await Task.CompletedTask;
    }

    public override async Task HandleAsync()
    {
        // Specific business logic for handling the event
        await _userRepository.SaveAsync();
    }

    public override async Task CompleteStepAsync()
    {
        // Commit the transaction
        await Task.CompletedTask;
    }

    public override async Task CompensateAsync()
    {
        // Rollback changes if HandleAsync or CompleteStepAsync failed
        await _userRepository.RollbackAsync();
    }
}
```

### Example 2: Custom Error Handling Strategy
This example illustrates overriding the virtual `HandleErrorAsync` method to implement a custom retry policy before falling back to compensation.

```csharp
public class ResilientOrderHandlers : EventHandlers
{
    private readonly ILogger _logger;
    private int _retryCount;

    public ResilientOrderHandlers(ILogger logger)
    {
        _logger = logger;
        RegisterHandlers();
    }

    public override void RegisterHandlers() { }

    public override Task StartAsync() => Task.CompletedTask;
    public override Task HandleAsync() => Task.CompletedTask; // Implementation omitted
    public override Task CompleteStepAsync() => Task.CompletedTask;
    public override Task CompensateAsync() => Task.CompletedTask;

    public override async Task HandleErrorAsync()
    {
        if (_retryCount < 3)
        {
            _retryCount++;
            _logger.LogWarning("Transient error detected. Retrying...");
            await Task.Delay(1000 * _retryCount);
            // Trigger retry logic specific to implementation
            return;
        }

        _logger.LogError("Max retries exceeded. Initiating compensation.");
        await CompensateAsync();
    }
}
```

## Notes

*   **Thread Safety**: The `EventHandlers` base class does not guarantee thread safety for its public members. Concrete implementations must ensure that shared state accessed within `HandleAsync`, `StartAsync`, and `CompensateAsync` is properly synchronized if instances are reused across concurrent threads.
*   **Execution Order**: The lifecycle methods follow a strict sequence: `StartAsync` is called first, followed by `HandleAsync`, and then `CompleteStepAsync`. If an exception occurs at any point, `HandleErrorAsync` is invoked, which may subsequently trigger `CompensateAsync`. Implementations should not assume `CompleteStepAsync` is called if `HandleAsync` throws.
*   **Exception Propagation**: While `HandleErrorAsync` provides a mechanism to swallow or manage exceptions, unhandled exceptions within the abstract lifecycle methods (`StartAsync`, `HandleAsync`, `CompleteStepAsync`, `CompensateAsync`) will propagate up the call stack. It is the responsibility of the caller or the framework hosting these handlers to manage top-level failures.
*   **Registration Timing**: The `RegisterHandlers` method should be called during construction or initialization before any events are dispatched to the instance. Failing to register handlers may result in events being ignored or runtime errors depending on the dispatcher implementation.
