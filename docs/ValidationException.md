# ValidationException

Represents an exception that occurs when domain validation fails, carrying a dictionary of property-level error messages. It provides factory methods for common validation failure scenarios and supports fluent aggregation of multiple errors into a single exception instance.

## API

### Properties

#### `ValidationErrors`
```csharp
public Dictionary<string, string> ValidationErrors
```
Gets the dictionary of validation errors where each key is a property name or error category and each value is the corresponding error message. This dictionary is never null; an exception with no specific property errors will contain an empty dictionary.

---

### Constructors

#### `ValidationException()`
```csharp
public ValidationException()
```
Initializes a new instance with an empty `ValidationErrors` dictionary and a default exception message. Suitable for scenarios where the exception is thrown immediately and errors are added fluently afterward, though typically one of the static factory methods is preferred.

#### `ValidationException(string message)`
```csharp
public ValidationException(string message)
```
Initializes a new instance with the specified message and an empty `ValidationErrors` dictionary.

**Parameters:**
- `message` — The exception message that describes the overall validation failure.

---

### Methods

#### `WithError(string key, string message)`
```csharp
public ValidationException WithError(string key, string message)
```
Adds a single validation error to the exception's `ValidationErrors` dictionary and returns the same instance for fluent chaining. If the key already exists, its value is overwritten.

**Parameters:**
- `key` — The property name or error category (e.g., `"Email"`, `"AggregateId"`).
- `message` — The human-readable error message associated with that key.

**Returns:** The current `ValidationException` instance, enabling multiple calls to be chained.

**Throws:**
- `ArgumentNullException` — if `key` or `message` is null.

---

### Static Factory Methods

#### `InvalidInput(string message)`
```csharp
public static ValidationException InvalidInput(string message)
```
Creates a `ValidationException` with the specified message and an empty `ValidationErrors` dictionary. Use this when the overall input is malformed but no specific property-level errors are identified.

**Parameters:**
- `message` — The exception message.

**Returns:** A new `ValidationException` instance.

#### `InvalidArgument(string paramName, string message)`
```csharp
public static ValidationException InvalidArgument(string paramName, string message)
```
Creates a `ValidationException` with a single error entry keyed by the parameter name. The exception message is set to the provided message.

**Parameters:**
- `paramName` — The name of the invalid argument.
- `message` — The error message describing why the argument is invalid.

**Returns:** A new `ValidationException` instance with one entry in `ValidationErrors`.

#### `AggregateValidationFailed(string aggregateId, string message)`
```csharp
public static ValidationException AggregateValidationFailed(string aggregateId, string message)
```
Creates a `ValidationException` indicating that validation of an event-sourced aggregate has failed. The error is keyed by the aggregate identifier.

**Parameters:**
- `aggregateId` — The identifier of the aggregate that failed validation.
- `message` — The error message describing the validation failure.

**Returns:** A new `ValidationException` instance with one entry in `ValidationErrors` keyed by the aggregate identifier.

---

## Usage

### Example 1: Validating a Command with Multiple Property Errors

```csharp
public void Handle(CreateOrderCommand command)
{
    var errors = new ValidationException("Order creation failed validation");

    if (string.IsNullOrWhiteSpace(command.CustomerId))
        errors.WithError("CustomerId", "Customer identifier is required.");

    if (command.Items == null || command.Items.Count == 0)
        errors.WithError("Items", "At least one order item is required.");

    if (command.TotalAmount <= 0)
        errors.WithError("TotalAmount", "Total amount must be greater than zero.");

    if (errors.ValidationErrors.Count > 0)
        throw errors;

    // Proceed with order creation...
}
```

### Example 2: Using Factory Methods for Specific Failure Scenarios

```csharp
public void ApplyEvent(OrderShippedEvent @event, OrderAggregate aggregate)
{
    if (aggregate.Status != OrderStatus.Confirmed)
    {
        throw ValidationException.AggregateValidationFailed(
            aggregate.Id,
            $"Cannot ship an order in '{aggregate.Status}' status. Order must be Confirmed.");
    }

    if (string.IsNullOrWhiteSpace(@event.TrackingNumber))
    {
        throw ValidationException.InvalidArgument(
            nameof(@event.TrackingNumber),
            "Tracking number must be provided when shipping an order.");
    }

    aggregate.Apply(@event);
}
```

---

## Notes

- **Dictionary Overwrites:** The `WithError` method overwrites existing values for duplicate keys without warning. If multiple errors for the same property must be preserved, combine them into a single message string before calling `WithError`, or use a different key scheme.
- **Empty Dictionary:** An exception created via the parameterless constructor or `InvalidInput` has an empty `ValidationErrors` dictionary. Calling code should check `ValidationErrors.Count` rather than nullity when determining whether property-level errors exist.
- **Thread Safety:** This class is not thread-safe. The `WithError` method mutates the internal dictionary and returns the same instance. Concurrent calls to `WithError` on the same exception instance from multiple threads will produce unpredictable results. Create separate instances per thread or synchronize externally if shared access is required.
- **Factory Methods vs. Constructor:** The static factory methods (`InvalidInput`, `InvalidArgument`, `AggregateValidationFailed`) provide semantically meaningful instantiation and automatically populate the error dictionary with the appropriate key. Prefer these over the raw constructors when the failure category is known at throw time.
- **Exception Inheritance:** `ValidationException` derives from `Exception` and integrates with standard .NET exception handling. Catch blocks targeting `ValidationException` can inspect `ValidationErrors` to return structured error responses to clients.
