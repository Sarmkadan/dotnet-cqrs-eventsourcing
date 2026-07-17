# ResultValidation

Utility class that provides static helpers for validating operation results. It enables callers to retrieve validation messages, check validity, and enforce validity through exceptions, with generic overloads that allow type‑specific validation contexts.

## API

### Validate
- **Purpose**: Returns a read‑only list of validation messages for the current result. An empty list indicates success.
- **Parameters**: None.
- **Return Value**: `IReadOnlyList<string>` containing zero or more error messages.
- **Exceptions**: None thrown under normal operation.

### Validate<T>
- **Purpose**: Returns a read‑only list of validation messages specific to type `T`. An empty list indicates success.
- **Parameters**: None.
- **Return Value**: `IReadOnlyList<string>` containing zero or more error messages relevant to `T`.
- **Exceptions**: None thrown under normal operation.

### IsValid
- **Purpose**: Determines whether the current result passes validation.
- **Parameters**: None.
- **Return Value**: `true` if the validation message list is empty; otherwise `false`.
- **Exceptions**: None thrown under normal operation.

### IsValid<T>
- **Purpose**: Determines whether the result of type `T` passes validation.
- **Parameters**: None.
- **Return Value**: `true` if the validation message list for `T` is empty; otherwise `false`.
- **Exceptions**: None thrown under normal operation.

### EnsureValid
- **Purpose**: Throws an exception if the current result is invalid, otherwise does nothing.
- **Parameters**: None.
- **Return Value**: `void`.
- **Exceptions**: Throws `InvalidOperationException` whose message aggregates all validation errors when `IsValid` is `false`.

### EnsureValid<T>
- **Purpose**: Throws an exception if the result of type `T` is invalid, otherwise does nothing.
- **Parameters**: None.
- **Return Value**: `void`.
- **Exceptions**: Throws `InvalidOperationException` whose message aggregates all validation errors for `T` when `IsValid<T>` is `false`.

## Usage

```csharp
// Example 1: Retrieve validation messages and act on them
var errors = ResultValidation.Validate();
if (errors.Count > 0)
{
    foreach (var err in errors)
    {
        logger.Warn(err);
    }
    // Optionally collect errors for a response
}
else
{
    // Proceed with normal processing
}
```

```csharp
// Example 2: Enforce validity of a typed result using the generic helper
ResultValidation.EnsureValid<OrderResult>();
// If OrderResult fails validation, an InvalidOperationException is thrown
// containing all validation messages; otherwise execution continues.
```

## Notes

- An empty list returned by any `Validate` or `Validate<T>` overload signifies a valid result; non‑empty lists contain the specific validation failures.
- The static methods contain no mutable state, making them safe to invoke concurrently from multiple threads.
- `EnsureValid` and `EnsureValid<T>` are intended for scenarios where a validation failure should abort the operation; they wrap the validation state in an `InvalidOperationException` to simplify error handling.
- If the underlying validation logic depends on external data, callers must ensure that data is stable for the duration of the validation call to avoid inconsistent results.
