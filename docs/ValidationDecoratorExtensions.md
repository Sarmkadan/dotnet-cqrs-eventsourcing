# ValidationDecoratorExtensions

Provides a set of extension methods for validating CQRS command objects and executing their handlers with optional validation logic. The methods enable a fluent, declarative approach to command validation, error reporting, and safe execution within an event‑sourced architecture.

## API

### Validate<TCommand, TResult>
**Purpose**  
Runs validation rules for the supplied command and collects any validation messages.

**Parameters**  
- `command` (TCommand): The command instance to validate.

**Return value**  
An `IReadOnlyList<string>` containing zero or more validation error messages. An empty list indicates that the command passed validation.

**Throws**  
- `ArgumentNullException` if `command` is `null`.

### ValidateOrThrow<TCommand, TResult>
**Purpose**  
Validates the command and aborts execution by throwing an exception if any validation errors are found.

**Parameters**  
- `command` (TCommand): The command instance to validate.

**Return value**  
None.

**Throws**  
- `ArgumentNullException` if `command` is `null`.  
- `ValidationException` (or a derived type) containing the aggregated validation messages when validation fails.

### ValidateOrReturn<TCommand, TResult>
**Purpose**  
Validates the command and returns the command instance when validation succeeds; otherwise returns the default value for `TCommand` (typically `null` for reference types).

**Parameters**  
- `command` (TCommand): The command instance to validate.

**Return value**  
The original `command` instance if validation passes; otherwise `default(TCommand)`.

**Throws**  
- `ArgumentNullException` if `command` is `null`.

### ExecuteWithValidationAsync<TCommand, TResult>
**Purpose**  
Validates the command and, if validation succeeds, invokes the associated command handler asynchronously, returning the handler's result.

**Parameters**  
- `command` (TCommand): The command to validate and execute.

**Return value**  
A `Task<TResult>` that completes with the result produced by the command handler.

**Throws**  
- `ArgumentNullException` if `command` is `null`.  
- `ValidationException` if validation fails (the handler is not invoked).  
- Any exception thrown by the command handler is propagated unchanged.

### ExecuteValidatedAsync<TCommand, TResult>
**Purpose**  
Executes the command handler asynchronously assuming the command has already been validated elsewhere. No validation is performed inside this method.

**Parameters**  
- `command` (TCommand): The pre‑validated command to execute.

**Return value**  
A `Task<TResult>` that completes with the result produced by the command handler.

**Throws**  
- `ArgumentNullException` if `command` is `null`.  
- Any exception thrown by the command handler is propagated unchanged.

### TryExecuteAsync<TCommand>
**Purpose**  
Attempts to validate and execute a command asynchronously, returning a boolean that indicates whether the operation succeeded without throwing validation exceptions.

**Parameters**  
- `command` (TCommand): The command to validate and execute.

**Return value**  
A `Task<bool>` that completes with `true` if validation succeeded and the handler executed without error; `false` if validation failed or the handler threw an exception.

**Throws**  
- `ArgumentNullException` if `command` is `null`.  
- Exceptions thrown by the command handler are caught and result in a `false` return value; they are not propagated.

## Usage

### Example 1: Conditional execution based on validation
```csharp
var command = new CreateAccountCommand { OwnerId = Guid.NewGuid(), InitialBalance = 100m };

var errors = command.Validate<CreateAccountCommand, AccountDto>();
if (errors.Any())
{
    // Log or return validation problems to the caller
    return BadRequest(errors);
}

// Validation passed – proceed with execution
var result = await command.ExecuteWithValidationAsync<CreateAccountCommand, AccountDto>();
return Ok(result);
```

### Example 2: Fire‑and‑forget with safe failure handling
```csharp
var command = new TransferFundsCommand { From = sourceId, To = destId, Amount = 50m };

bool succeeded = await command.TryExecuteAsync<TransferFundsCommand>();
if (!succeeded)
{
    // Handle failure (e.g., retry, dead‑letter, or alert)
    logger.Warning("Transfer command failed validation or handler error.");
}
else
{
    logger.Information("Transfer command processed successfully.");
}
```

## Notes
- All extension methods are stateless; they rely solely on the supplied command instance and any registered validators/handlers. Consequently, they are safe to invoke concurrently from multiple threads, provided the command object itself is not mutated during validation or execution.
- If a command implements reference‑type semantics, `ValidateOrReturn` returns `null` when validation fails; callers should check for `null` before using the returned value.
- `TryExecuteAsync` swallows exceptions from the command handler to preserve its boolean contract; diagnostic information about such exceptions must be obtained through logging or other observability mechanisms external to the method.
- The generic type `TResult` represents the expected output of the command handler; it does not influence validation logic and is only used for the return types of the execution methods.
