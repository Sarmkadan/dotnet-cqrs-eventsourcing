# Result

A generic result type used to encapsulate the outcome of operations, distinguishing between success and failure states with optional data and error information. It provides functional-style operations for composing results and handling errors without exceptions.

## API

### `bool IsSuccess`
Indicates whether the result represents a successful operation. Returns `true` if the result is successful, `false` otherwise.

### `T? Data`
Gets the successful result data when `IsSuccess` is `true`. Returns `null` if the result represents a failure.

### `string? ErrorCode`
Gets the error code associated with the failure when `IsSuccess` is `false`. Returns `null` if no error code was provided.

### `string? ErrorMessage`
Gets the primary error message associated with the failure when `IsSuccess` is `false`. Returns `null` if no error message was provided.

### `List<string> Errors`
Gets the collection of error messages associated with the failure when `IsSuccess` is `false`. Empty if no errors were added.

### `static Result<T> Success(T data)`
Creates a successful result with the provided data.

**Parameters:**
- `data`: The successful result data.

**Return Value:**
A new `Result<T>` instance with `IsSuccess` set to `true` and the provided data.

### `static Result<T> Failure(string errorCode, string errorMessage)`
Creates a failed result with the specified error code and message.

**Parameters:**
- `errorCode`: The error code identifying the failure type.
- `errorMessage`: The primary error message describing the failure.

**Return Value:**
A new `Result<T>` instance with `IsSuccess` set to `false` and the provided error information.

### `static Result<T> Failure(string errorMessage)`
Creates a failed result with the specified error message.

**Parameters:**
- `errorMessage`: The primary error message describing the failure.

**Return Value:**
A new `Result<T>` instance with `IsSuccess` set to `false` and the provided error message.

### `void AddError(string error)`
Adds an additional error message to the result when `IsSuccess` is `false`.

**Parameters:**
- `error`: The error message to add.

**Throws:**
- `InvalidOperationException`: If `IsSuccess` is `true`.

### `TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, IEnumerable<string>, TOut> onFailure)`
Projects the result into a value of type `TOut` based on whether the operation succeeded or failed.

**Parameters:**
- `onSuccess`: Function to invoke if the result is successful.
- `onFailure`: Function to invoke if the result is a failure.

**Return Value:**
The result of the invoked function.

**Throws:**
- `ArgumentNullException`: If `onSuccess` or `onFailure` is `null`.

### `void Match(Action<T> onSuccess, Action<string, IEnumerable<string>> onFailure)`
Projects the result into side effects based on whether the operation succeeded or failed.

**Parameters:**
- `onSuccess`: Action to invoke if the result is successful.
- `onFailure`: Action to invoke if the result is a failure.

**Throws:**
- `ArgumentNullException`: If `onSuccess` or `onFailure` is `null`.

### `void ThrowIfFailure()`
Throws an exception if the result represents a failure.

**Throws:**
- `InvalidOperationException`: If `IsSuccess` is `false`, containing the error message and errors.

### `Result<TOut> MapSuccess<TOut>(Func<T, TOut> mapper)`
Transforms the successful result data into a new value if the result is successful.

**Parameters:**
- `mapper`: Function to transform the successful data.

**Return Value:**
A new `Result<TOut>` instance with the transformed data if successful, or the original failure otherwise.

**Throws:**
- `ArgumentNullException`: If `mapper` is `null`.

### `override string ToString()`
Returns a string representation of the result, including its success state, data (if present), and error information (if present).

**Return Value:**
A string describing the result.

## Usage

```csharp
// Example 1: Basic success and failure handling
Result<int> result = ValidateAge(15);

if (result.IsSuccess)
{
    Console.WriteLine($"Valid age: {result.Data}");
}
else
{
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
}

// Example 2: Chaining operations with MapSuccess
Result<string> formattedResult = ValidateAge(25)
    .MapSuccess(age => $"Age is {age}");

formattedResult.Match(
    onSuccess: formatted => Console.WriteLine(formatted),
    onFailure: (code, errors) => Console.WriteLine($"Error: {code} - {string.Join(", ", errors)}")
);
```

## Notes

- The `AddError` method throws if called on a successful result, enforcing the invariant that errors can only be added to failed results.
- The `Match` methods require non-null delegates; passing `null` will throw an `ArgumentNullException`.
- The `MapSuccess` method does not execute the mapper if the result is a failure, allowing safe composition without side effects.
- Thread safety is not guaranteed for mutation operations like `AddError`; concurrent modifications may lead to inconsistent state. The type is intended for single-threaded or externally synchronized use.
