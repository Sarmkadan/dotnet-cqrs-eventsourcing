# CqrsExceptionExtensions
The `CqrsExceptionExtensions` class provides a set of extension methods for working with `CqrsException` objects, allowing for the creation and modification of exceptions in a more fluent and expressive manner. These extensions enable developers to easily add additional context to exceptions, such as correlation IDs, occurrence times, and error codes, making it easier to diagnose and handle errors in CQRS-based systems.

## API
* `public static CqrsException WithCorrelationId(this CqrsException exception, Guid correlationId)`: Adds a correlation ID to the specified `CqrsException`. The `correlationId` parameter is the ID to be added. The method returns the modified `CqrsException` object.
* `public static CqrsException WithOccurredAt(this CqrsException exception, DateTimeOffset occurredAt)`: Sets the occurrence time of the specified `CqrsException`. The `occurredAt` parameter is the time at which the exception occurred. The method returns the modified `CqrsException` object.
* `public static CqrsException WithErrorCode(this CqrsException exception, string errorCode)`: Adds an error code to the specified `CqrsException`. The `errorCode` parameter is the code to be added. The method returns the modified `CqrsException` object.
* `public static AggregateNotFoundException ToAggregateNotFoundException(this CqrsException exception)`: Converts the specified `CqrsException` to an `AggregateNotFoundException`. The method returns the converted exception.

## Usage
The following examples demonstrate how to use the `CqrsExceptionExtensions` class:
```csharp
// Create a new CqrsException and add a correlation ID
var exception = new CqrsException("Test exception").WithCorrelationId(Guid.NewGuid());

// Create a new CqrsException, set its occurrence time, and add an error code
var anotherException = new CqrsException("Another test exception")
    .WithOccurredAt(DateTimeOffset.Now)
    .WithErrorCode("ERROR-123");
```

## Notes
When using the `CqrsExceptionExtensions` class, note that the `WithCorrelationId`, `WithOccurredAt`, and `WithErrorCode` methods modify the original `CqrsException` object and return the modified object. This allows for method chaining, but also means that the original object is changed. The `ToAggregateNotFoundException` method creates a new exception object and does not modify the original. All methods are thread-safe, as they only operate on the input `CqrsException` object and do not rely on any shared state. However, the thread-safety of the `CqrsException` object itself depends on its implementation.
