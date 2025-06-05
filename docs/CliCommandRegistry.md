# CliCommandRegistry

The `CliCommandRegistry` class serves as the central command orchestrator for the CLI subsystem within the `dotnet-cqrs-eventsourcing` framework. It provides a structured mechanism for registering, discovering, resolving, and invoking CLI commands, ensuring a clean separation between command definitions and their execution logic.

## API

### CliCommandRegistry
Initializes a new instance of the `CliCommandRegistry` class.

### TryResolve
```csharp
public bool TryResolve(string commandName, out ICliCommand? command)
```
Attempts to resolve a command based on the provided `commandName`.
*   **Parameters**: 
    *   `commandName`: The name of the command to resolve.
    *   `command`: When this method returns, contains the resolved `ICliCommand` if found; otherwise, `null`.
*   **Returns**: `true` if the command was successfully resolved; otherwise, `false`.

### DispatchAsync
```csharp
public async Task<Result> DispatchAsync(ICliCommand command, CancellationToken ct = default)
```
Dispatches a resolved command for asynchronous execution.
*   **Parameters**:
    *   `command`: The `ICliCommand` instance to execute.
    *   `ct`: A `CancellationToken` to monitor for cancellation requests.
*   **Returns**: A `Task<Result>` representing the asynchronous operation, containing the execution outcome.
*   **Throws**: Throws if the command execution fails or encounters a critical error.

### PrintHelp
```csharp
public void PrintHelp()
```
Outputs the help documentation for all currently registered commands to the standard output.

## Usage

### Example 1: Resolving and Dispatching a Command
This example demonstrates how to resolve a command by name and execute it if found.

```csharp
var registry = new CliCommandRegistry();

if (registry.TryResolve("rebuild-read-model", out var command))
{
    var result = await registry.DispatchAsync(command);
    if (result.IsSuccess)
    {
        Console.WriteLine("Command executed successfully.");
    }
}
else
{
    Console.WriteLine("Command not found.");
}
```

### Example 2: Displaying Available Commands
This example demonstrates how to display the available commands to the user.

```csharp
var registry = new CliCommandRegistry();
registry.PrintHelp();
```

## Notes

*   **Thread Safety**: The `CliCommandRegistry` is designed for scenarios where command resolution and dispatch occur after the registry has been fully initialized. Concurrent modification of the registry during resolution or dispatch is not thread-safe.
*   **Result Pattern**: The `DispatchAsync` method returns a `Result` type, which is expected to be part of the `dotnet-cqrs-eventsourcing.Shared.Results` namespace, indicating the success or failure of the operation along with any associated error details.
*   **Resolution**: The `TryResolve` method is case-sensitive or case-insensitive depending on the underlying implementation details; command names should generally be matched as defined during registration.
