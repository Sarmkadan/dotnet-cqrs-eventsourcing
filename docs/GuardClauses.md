# GuardClauses

The `GuardClauses` static class provides a set of input validation helpers commonly used in domain-driven design and command/query handling. Each method evaluates a single precondition and throws an appropriate exception (typically `ArgumentNullException`, `ArgumentException`, or `ArgumentOutOfRangeException`) when the condition is not met, allowing the caller to fail fast with a clear error message. The methods are designed to be used inline at the start of constructors, methods, or property setters.

## API

### `public static T NotNull<T>(T value, string parameterName)`

- **Purpose**: Ensures that a reference type or nullable value type argument is not `null`.
- **Parameters**:
  - `value` – The argument to validate.
  - `parameterName` – The name of the parameter (used in the exception message).
- **Returns**: The original `value` if it is not `null`.
- **Throws**: `ArgumentNullException` when `value` is `null`.

### `public static string NotNullOrEmpty(string value, string parameterName)`

- **Purpose**: Ensures that a string argument is not `null` and not empty (`""`).
- **Parameters**:
  - `value` – The string to validate.
  - `parameterName` – The name of the parameter.
- **Returns**: The original `value` if it is not `null` or empty.
- **Throws**: `ArgumentNullException` if `value` is `null`; `ArgumentException` if `value` is empty.

### `public static IEnumerable<T> NotNullOrEmpty<T>(IEnumerable<T> value, string parameterName)`

- **Purpose**: Ensures that a collection argument is not `null` and contains at least one element.
- **Parameters**:
  - `value` – The collection to validate.
  - `parameterName` – The name of the parameter.
- **Returns**: The original `value` if it is not `null` and not empty.
- **Throws**: `ArgumentNullException` if `value` is `null`; `ArgumentException` if the collection contains no elements.

### `public static T InRange<T>(T value, string parameterName, T minimum, T maximum) where T : IComparable<T>`

- **Purpose**: Ensures that a comparable value falls within the specified inclusive range.
- **Parameters**:
  - `value` – The value to validate.
  - `parameterName` – The name of the parameter.
  - `minimum` – The lower bound of the range (inclusive).
  - `maximum` – The upper bound of the range (inclusive).
- **Returns**: The original `value` if it is within the range.
- **Throws**: `ArgumentOutOfRangeException` when `value` is less than `minimum` or greater than `maximum`.

### `public static T NotNegative<T>(T value, string parameterName) where T : IComparable<T>`

- **Purpose**: Ensures that a comparable value is not negative (i.e., greater than or equal to zero).
- **Parameters**:
  - `value` – The value to validate.
  - `parameterName` – The name of the parameter.
- **Returns**: The original `value` if it is not negative.
- **Throws**: `ArgumentOutOfRangeException` when `value` is less than zero.

### `public static T NotZero<T>(T value, string parameterName) where T : IComparable<T>`

- **Purpose**: Ensures that a comparable value is not equal to zero.
- **Parameters**:
  - `value` – The value to validate.
  - `parameterName` – The name of the parameter.
- **Returns**: The original `value` if it is not zero.
- **Throws**: `ArgumentOutOfRangeException` when `value` equals zero.

### `public static void Condition(bool condition, string parameterName, string message)`

- **Purpose**: Evaluates a boolean expression and throws if the condition is `false`.
- **Parameters**:
  - `condition` – The boolean expression to validate.
  - `parameterName` – The name of the parameter associated with the condition.
  - `message` – A custom error message describing the violation.
- **Returns**: Nothing.
- **Throws**: `ArgumentException` when `condition` is `false`.

### `public static Guid NotEmpty(Guid value, string parameterName)`

- **Purpose**: Ensures that a `Guid` argument is not equal to `Guid.Empty`.
- **Parameters**:
  - `value` – The GUID to validate.
  - `parameterName` – The name of the parameter.
- **Returns**: The original `value` if it is not empty.
- **Throws**: `ArgumentException` when `value` equals `Guid.Empty`.

### `public static string Matches(string value, string parameterName, string pattern)`

- **Purpose**: Ensures that a string argument matches a specified regular expression pattern.
- **Parameters**:
  - `value` – The string to validate.
  - `parameterName` – The name of the parameter.
  - `pattern` – The regular expression pattern to match against.
- **Returns**: The original `value` if it matches the pattern.
- **Throws**: `ArgumentException` when `value` does not match `pattern`; `ArgumentNullException` if `value` is `null`.

## Usage

### Example 1: Validating constructor parameters for an entity

```csharp
public class Customer
{
    public Customer(string name, int age, Guid id)
    {
        Name = GuardClauses.NotNullOrEmpty(name, nameof(name));
        Age = GuardClauses.InRange(age, nameof(age), 0, 120);
        Id = GuardClauses.NotEmpty(id, nameof(id));
    }

    public string Name { get; }
    public int Age { get; }
    public Guid Id { get; }
}
```

### Example 2: Guarding a command handler against invalid input

```csharp
public class CreateOrderHandler
{
    public void Handle(CreateOrderCommand command)
    {
        GuardClauses.NotNull(command, nameof(command));
        GuardClauses.NotNullOrEmpty(command.Items, nameof(command.Items));
        GuardClauses.Condition(command.Total > 0, nameof(command.Total), "Total must be positive.");
        GuardClauses.Matches(command.Email, nameof(command.Email), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        // Business logic follows...
    }
}
```

## Notes

- All methods are static and operate only on their input parameters; they do not modify any shared state. Consequently, they are inherently thread-safe and can be called concurrently from multiple threads without synchronization.
- When a guard fails, the thrown exception includes the `parameterName` to aid debugging. The exact exception type varies by method: `ArgumentNullException` for null checks, `ArgumentException` for empty/pattern/condition failures, and `ArgumentOutOfRangeException` for range and zero/negative checks.
- The generic methods `InRange`, `NotNegative`, and `NotZero` require the type parameter `T` to implement `IComparable<T>`. Using a non-comparable type will result in a compile-time error.
- For `NotNullOrEmpty<T>` (collection), the emptiness check is performed by enumerating the collection. If the collection is a lazy sequence (e.g., an `IEnumerable<T>` backed by a database query), the enumeration may have side effects or cause performance issues. Prefer passing materialized collections (e.g., `List<T>`, array) when using this guard.
- The `Matches` method uses the regular expression engine under the default settings (single-line, culture-invariant). The pattern is expected to be a valid regex; an invalid pattern will throw a `RegexParseException` at runtime.
- Edge cases: `NotNegative` and `NotZero` accept any `IComparable<T>`, including floating-point types. For `float` and `double`, negative zero is treated as zero; `NotZero` will throw for `-0.0` as well. `InRange` uses the `CompareTo` method, so the comparison semantics depend on the type’s implementation (e.g., `DateTime` uses chronological ordering).
