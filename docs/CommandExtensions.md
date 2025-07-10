# CommandExtensions

A utility class providing extension methods for command handling in CQRS and event-sourcing scenarios. It offers methods for command validation, event enrichment, correlation management, and result transformations, enabling consistent patterns for command execution and state management.

## API

### `public static async Task<Result<T>> ExecuteCommandAsync<T>(this ICommandHandler<T> handler, ICommand<T> command)`

Executes a command asynchronously using the provided `ICommandHandler<T>`. The command is validated before execution, and the handler processes it to produce a result.

- **Parameters**:
  - `handler`: The command handler implementing `ICommandHandler<T>`.
  - `command`: The command to execute, implementing `ICommand<T>`.
- **Return value**: A `Task<Result<T>>` representing the asynchronous operation, containing the result of the command execution or validation errors.
- **Exceptions**: Throws `ArgumentNullException` if `handler` or `command` is `null`.

---

### `public static string GetOrCreateCorrelationId()`

Generates a new correlation ID if none exists in the current execution context, or retrieves the existing one. Useful for tracing command executions and associated events.

- **Return value**: A string representing the correlation ID.
- **Exceptions**: None.

---

### `public static TEvent EnrichEvent<TEvent>(this IEventEnricher<TEvent> enricher, TEvent @event)`

Enriches an event with additional metadata using the provided `IEventEnricher<TEvent>`. Enrichment may include correlation IDs, timestamps, or other contextual data.

- **Parameters**:
  - `enricher`: The enricher implementing `IEventEnricher<TEvent>`.
  - `@event`: The event to enrich.
- **Return value**: The enriched event of type `TEvent`.
- **Exceptions**: Throws `ArgumentNullException` if `enricher` or `@event` is `null`.

---

### `public static ICollection<string> Validate<T>(this IValidator<T> validator, T instance)`

Validates an instance of type `T` using the provided `IValidator<T>`. Returns a collection of validation error messages if validation fails.

- **Parameters**:
  - `validator`: The validator implementing `IValidator<T>`.
  - `instance`: The instance to validate.
- **Return value**: An `ICollection<string>` of validation error messages. Empty if validation succeeds.
- **Exceptions**: Throws `ArgumentNullException` if `validator` or `instance` is `null`.

---
### `public static TEvent CreateEventFromCommand<TCommand, TEvent>(this ICommandToEventMapper<TCommand, TEvent> mapper, TCommand command)`

Maps a command of type `TCommand` to an event of type `TEvent` using the provided `ICommandToEventMapper<TCommand, TEvent>`. Useful for converting commands into their corresponding events in CQRS architectures.

- **Parameters**:
  - `mapper`: The mapper implementing `ICommandToEventMapper<TCommand, TEvent>`.
  - `command`: The command to map.
- **Return value**: The resulting event of type `TEvent`.
- **Exceptions**: Throws `ArgumentNullException` if `mapper` or `command` is `null`.

---
### `public static string GetEventDisplayName(Type eventType)`

Retrieves a human-readable display name for the given event type. Useful for logging, debugging, or UI representations.

- **Parameters**:
  - `eventType`: The type of the event.
- **Return value**: A string representing the display name of the event type.
- **Exceptions**: Throws `ArgumentNullException` if `eventType` is `null`.

---
### `public static object GetEventSummary(object @event)`

Extracts a summary object from the given event. The summary may include key properties or a condensed representation of the event.

- **Parameters**:
  - `@event`: The event from which to extract the summary.
- **Return value**: An object representing a summary of the event.
- **Exceptions**: Throws `ArgumentNullException` if `@event` is `null`.

---
### `public static void ValidateStateForOperation(object aggregate, string operationName)`

Validates the state of an aggregate before performing an operation. Throws an exception if the aggregate is in an invalid state for the specified operation.

- **Parameters**:
  - `aggregate`: The aggregate to validate.
  - `operationName`: The name of the operation being performed (used in error messages).
- **Exceptions**: Throws `ArgumentNullException` if `aggregate` is `null`. Throws `InvalidOperationException` if the aggregate state is invalid for the operation.

---
### `public static object GetAggregateSummary(object aggregate)`

Extracts a summary object from the given aggregate. The summary may include key properties or a condensed representation of the aggregate state.

- **Parameters**:
  - `aggregate`: The aggregate from which to extract the summary.
- **Return value**: An object representing a summary of the aggregate.
- **Exceptions**: Throws `ArgumentNullException` if `aggregate` is `null`.

---
### `public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)`

Transforms the value inside a `Result<TIn>` using the provided mapper function. If the result is a failure, the mapper is not invoked.

- **Parameters**:
  - `result`: The result to transform.
  - `mapper`: A function to apply to the value if the result is successful.
- **Return value**: A new `Result<TOut>` containing the mapped value or the original failure.
- **Exceptions**: Throws `ArgumentNullException` if `result` or `mapper` is `null`.

---
### `public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)`

Asynchronously binds the value inside a `Result<TIn>` to a new `Result<TOut>` using the provided binder function. Useful for chaining asynchronous operations that may fail.

- **Parameters**:
  - `result`: The result to bind.
  - `binder`: An asynchronous function to apply to the value if the result is successful.
- **Return value**: A `Task<Result<TOut>>` representing the asynchronous binding operation.
- **Exceptions**: Throws `ArgumentNullException` if `result` or `binder` is `null`.

---
### `public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)`

Asynchronously performs a side effect on the value inside a `Result<T>` if it is successful. Useful for logging or other side effects without altering the result.

- **Parameters**:
  - `result`: The result on which to perform the side effect.
  - `action`: An asynchronous function to apply to the value if the result is successful.
- **Return value**: A `Task<Result<T>>` representing the asynchronous operation, containing the original result.
- **Exceptions**: Throws `ArgumentNullException` if `result` or `action` is `null`.

## Usage

### Example 1: Executing a Command with Validation and Enrichment
