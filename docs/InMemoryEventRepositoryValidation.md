# InMemoryEventRepositoryValidation

Static utility class that provides validation helpers for an in‑memory event repository implementation. It allows callers to check whether the repository is correctly configured, retrieve a list of specific validation problems, or enforce validity by throwing an exception when issues are found.

## API

### `public static IReadOnlyList<string> Validate()`

**Purpose**  
Performs validation of the in‑memory event repository configuration and returns any problems found.

**Parameters**  
None.

**Return value**  
An `IReadOnlyList<string>` containing zero or more error messages. An empty list indicates that the repository configuration is valid.

**Exceptions**  
This method does not throw exceptions under normal operation. If an unexpected internal error occurs (e.g., a null reference caused by corrupted state), it may propagate the underlying exception.

---

### `public static bool IsValid()`

**Purpose**  
Determines whether the in‑memory event repository configuration is valid.

**Parameters**  
None.

**Return value**  
`true` if `Validate()` returns an empty list; otherwise `false`.

**Exceptions**  
None.

---

### `public static void EnsureValid()`

**Purpose**  
Validates the repository configuration and throws an exception if any problems are detected.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` when `Validate()` returns a non‑empty list. The exception message concatenates all validation errors, separated by newlines. If the validation list is empty, the method completes normally.

---

### `public static IReadOnlyList<string> Validate<T>()`

**Purpose**  
Performs type‑specific validation for events of type `T` within the in‑memory event repository (e.g., ensuring that handlers or snapshots are registered for `T`).

**Parameters**  
None; the generic type argument `T` specifies the event type to validate.

**Return value**  
An `IReadOnlyList<string>` of validation messages concerning `T`. An empty list means the repository is correctly set up for events of type `T`.

**Exceptions**  
Same as the non‑generic `Validate()`: no expected exceptions; unexpected errors propagate as‑is.

---

### `public static bool IsValid<T>()`

**Purpose**  
Checks whether the repository is valid for events of type `T`.

**Parameters**  
None; `T` is the event type to check.

**Return value**  
`true` if `Validate<T>()` returns an empty list; otherwise `false`.

**Exceptions**  
None.

---

### `public static void EnsureValid<T>()`

**Purpose**  
Ensures that the repository is correctly configured for events of type `T`, throwing an T‑exception if validation fails.

**Parameters**  
None; `T` specifies the event type.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` when `Validate<T>()` returns a non‑empty list, with a message containing all validation errors for `T`. If validation passes, the method returns without throwing.

## Usage

### Basic validation of the repository

```csharp
using DotNetCqrs.EventSourcing;

// Assume the repository has been configured elsewhere.
if (!InMemoryEventRepositoryValidation.IsValid())
{
    var errors = InMemoryEventRepositoryValidation.Validate();
    foreach var error in errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
    // Handle misconfiguration (e.g., abort startup).
}
else
{
    // Proceed – the repository is ready to use.
}
```

### Enforcing validity for a specific event type

```csharp
using DotNetCqrs.EventSourcing;

// Validate that the repository can handle OrderCreated events.
InMemoryEventRepositoryValidation.EnsureValid<OrderCreated>();

// If the above line does not throw, the repository is correctly set up
// for persisting and retrieving OrderCreated events.
```

## Notes

- The class is stateless; all members are static and operate on the current global configuration of the in‑memory event repository. Consequently, the methods are thread‑safe and can be called concurrently from multiple threads without additional synchronization.
- Validation results are based solely on the state of the repository at the moment the method is invoked. If the repository configuration is changed after a call, subsequent calls may produce different outcomes.
- The generic members (`Validate<T>`, `IsValid<T>`, `EnsureValid<T>`) allow fine‑grained checks for particular event types, which is useful when different parts of the system rely on different subsets of events.
- If the underlying repository throws during validation (e.g., due to a null reference caused by mis‑initialization), that exception will propagate unchanged; callers should handle such unexpected errors according to their application’s error‑handling policy.
