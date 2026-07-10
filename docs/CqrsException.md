# CqrsException
The `CqrsException` type represents an exception that occurs within the context of a CQRS (Command Query Responsibility Segregation) and event sourcing system. It provides a standardized way to handle and propagate errors that may arise during the execution of commands, queries, or event handling, allowing for more robust and fault-tolerant system design.

## API
* `public string ErrorCode`: Gets the error code associated with the exception.
* `public string? CorrelationId`: Gets the correlation ID associated with the exception, if any.
* `public DateTime OccurredAt`: Gets the date and time when the exception occurred.
* `public CqrsException()`: Initializes a new instance of the `CqrsException` class.
* `public CqrsException(string errorCode, string? correlationId = default, DateTime occurredAt = default)`: Initializes a new instance of the `CqrsException` class with the specified error code, correlation ID, and occurrence time.
* `public AggregateNotFoundException(string aggregateId, string errorCode, string? correlationId = default, DateTime occurredAt = default)`: Initializes a new instance of the `AggregateNotFoundException` class, which is a subclass of `CqrsException`.
* `public EventStreamException(string eventStreamId, string errorCode, string? correlationId = default, DateTime occurredAt = default)`: Initializes a new instance of the `EventStreamException` class, which is a subclass of `CqrsException`.
* `public EventStreamException(string eventStreamId, Exception innerException, string errorCode, string? correlationId = default, DateTime occurredAt = default)`: Initializes a new instance of the `EventStreamException` class with the specified event stream ID, inner exception, error code, correlation ID, and occurrence time.

## Usage
The following examples demonstrate how to use the `CqrsException` type in a CQRS and event sourcing system:
```csharp
// Example 1: Throwing a CqrsException
public void HandleCommand(MyCommand command)
{
    try
    {
        // Command handling logic
    }
    catch (Exception ex)
    {
        throw new CqrsException("MY_ERROR_CODE", "MY_CORRELATION_ID", DateTime.UtcNow);
    }
}

// Example 2: Catching and handling a CqrsException
public void HandleQuery(MyQuery query)
{
    try
    {
        // Query handling logic
    }
    catch (CqrsException ex)
    {
        Console.WriteLine($"Error {ex.ErrorCode} occurred at {ex.OccurredAt} with correlation ID {ex.CorrelationId}");
        // Handle the exception accordingly
    }
}
```

## Notes
When using the `CqrsException` type, consider the following edge cases and thread-safety remarks:
* The `OccurredAt` property represents the date and time when the exception occurred, which can be useful for auditing and logging purposes.
* The `CorrelationId` property can be used to correlate exceptions across different parts of the system, facilitating error tracking and debugging.
* The `CqrsException` class is designed to be thread-safe, allowing it to be safely used in concurrent environments.
* When throwing a `CqrsException`, ensure that the error code and correlation ID are properly set to provide meaningful information for error handling and debugging.
* When catching a `CqrsException`, consider logging the exception details, including the error code, correlation ID, and occurrence time, to facilitate error tracking and analysis.
