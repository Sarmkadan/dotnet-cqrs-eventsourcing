# CqrsHelpers

Utility class providing reflection-based discovery and metadata extraction for CQRS command and event handlers, aggregate types, and command validation in a .NET event-sourcing context.

## API

### `public static IEnumerable<Type> GetCommandHandlers()`

Discovers all concrete types implementing `ICommandHandler<TCommand>` in the current application domain.

- **Returns**: An enumerable of `Type` objects representing all command handler implementations found.
- **Throws**: `InvalidOperationException` if type resolution fails due to assembly loading issues.

---

### `public static IEnumerable<Type> GetEventHandlers()`

Discovers all concrete types implementing `IEventHandler<TEvent>` in the current application domain.

- **Returns**: An enumerable of `Type` objects representing all event handler implementations found.
- **Throws**: `InvalidOperationException` if type resolution fails due to assembly loading issues.

---
### `public static HandlerMetadata GetHandlerMetadata(Type handlerType)`

Extracts metadata (e.g., supported command/event types) from a given handler type.

- **Parameters**:
  - `handlerType` (Type): The handler type to inspect.
- **Returns**: A `HandlerMetadata` instance containing command/event type information and display attributes.
- **Throws**:
  - `ArgumentNullException` if `handlerType` is `null`.
  - `InvalidOperationException` if the handler type does not conform to expected interfaces or lacks required attributes.

---
### `public static void RegisterEventType(Type eventType)`

Registers a custom event type for use in event sourcing pipelines.

- **Parameters**:
  - `eventType` (Type): The event type to register.
- **Throws**:
  - `ArgumentNullException` if `eventType` is `null`.
  - `InvalidOperationException` if the type is not a valid event (e.g., lacks `IEvent` marker or is abstract).

---
### `public static Type? ResolveEventType(string eventTypeName)`

Resolves an event type by its fully qualified name.

- **Parameters**:
  - `eventTypeName` (string): The fully qualified name of the event type.
- **Returns**: The resolved `Type` if found; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `eventTypeName` is `null`.

---
### `public static string? ExtractAggregateId(object command)`

Extracts the aggregate identifier from a command object using reflection.

- **Parameters**:
  - `command` (object): The command instance to inspect.
- **Returns**: The aggregate ID as a string if found via `[AggregateId]` attribute or convention; otherwise, `null`.
- **Throws**: `ArgumentNullException` if `command` is `null`.

---
### `public static Type? GetTargetAggregateType(Type commandType)`

Determines the aggregate type targeted by a given command type.

- **Parameters**:
  - `commandType` (Type): The command type to analyze.
- **Returns**: The `Type` of the aggregate if inferred from handler or attribute; otherwise, `null`.
- **Throws**:
  - `ArgumentNullException` if `commandType` is `null`.
  - `InvalidOperationException` if the command type is not associated with any known aggregate.

---
### `public static ICollection<string> ValidateCommand(object command)`

Validates a command object using reflection and registered validators.

- **Parameters**:
  - `command` (object): The command instance to validate.
- **Returns**: A collection of validation error messages; empty if valid.
- **Throws**: `ArgumentNullException` if `command` is `null`.

---
### `public static void ClearCaches()`

Clears internal reflection and metadata caches used by the helper methods.

- **Notes**: Safe to call at runtime; subsequent calls will rebuild caches as needed.

---
### `public required Type CommandType`

Gets or sets the command type associated with a handler or context.

- **Remarks**: Required for metadata generation and routing.

---
### `public required string DisplayName`

Gets or sets a human-readable name for the command or event type.

- **Remarks**: Used in logs, diagnostics, and UI.

---
### `public PropertyInfo[] Properties`

Gets the public properties of the associated command or event type.

- **Remarks**: Used for serialization, validation, and reflection-based inspection.

## Usage

### Discovering and Registering Handlers
