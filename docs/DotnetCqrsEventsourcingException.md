# DotnetCqrsEventsourcingException

Represents an error that occurs during CQRS event sourcing operations, capturing an application‑specific error code and the UTC timestamp of when the exception was raised.

## API

### ErrorCode
```csharp
public string ErrorCode { get; }
```
Gets the error code associated with the exception. The value is set at construction time and cannot be changed afterward. If no code was supplied, the property returns `null`.

### OccurredAt
```csharp
public DateTime OccurredAt { get; }
```
Gets the date and time (in UTC) when the exception occurred. The value is set at construction time; if not supplied, it defaults to `DateTime.UtcNow`.

### DotnetCqrsEventsourcingException()
```csharp
public DotnetCqrsEventsourcingException()
```
Initializes a new instance of the `DotnetCqrsEventsourcingException` class with default values (`ErrorCode` = `null`, `OccurredAt` = current UTC time). This constructor does not throw exceptions.

### DotnetCqrsEventsourcingException(string errorCode, DateTime occurredAt)
```csharp
public DotnetCqrsEventsourcingException(string errorCode, DateTime occurredAt)
```
Initializes a new instance with the supplied error code and timestamp.

- **Parameters**  
  - `errorCode`: A string that identifies the error. May be `null`, but if `null` is passed, the constructor throws `ArgumentNullException`.  
  - `occurredAt`: The UTC timestamp of the error. If the value equals `DateTime.MinValue`, the constructor throws `ArgumentOutOfRangeException`.

- **Return Value**  
  Returns a new `DotnetCqrsEventsourcingException` instance.

- **Exceptions**  
  - `ArgumentNullException` if `errorCode` is `null`.  
  - `ArgumentOutOfRangeException` if `occurredAt` is `DateTime.MinValue`.

### ToString()
```csharp
public override string ToString()
```
Returns a string that includes the error code, timestamp, and the base exception message. The format is:  
`"[ErrorCode: {ErrorCode}] [OccurredAt: {OccurredAt:O}] {base.ToString()}"`.  
This method does not throw exceptions.

## Usage

### Throwing the exception with custom data
```csharp
if (string.IsNullOrWhiteSpace(command.Id))
{
    throw new DotnetCqrsEventsourcingException(
        errorCode: "ERR_INVALID_COMMAND_ID",
        occurredAt: DateTime.UtcNow);
}
```

### Catching and inspecting the exception
```csharp
try
{
    eventStore.Append(event);
}
catch (DotnetCqrsEventsourcingException ex)
{
    // Log the structured information
    logger.Error(
        "Event sourcing failed: ErrorCode={ErrorCode}, OccurredAt={OccurredAt}",
        ex.ErrorCode,
        ex.OccurredAt);

    // Optionally rethrow or handle
    throw;
}
```

## Notes
- The exception is immutable after construction; all properties are read‑only, making it safe to publish or share across threads without additional threads without synchronization.
- If the parameterless constructor is used, `ErrorCode` will be `null` and `OccurredAt` will reflect the exact moment the instance was created.
- Passing `DateTime.MinValue` to the constructor is considered invalid and will result in an `ArgumentOutOfRangeException`; this guards against accidental use of an uninitialized `DateTime` value.
- The `ToString` implementation includes the ISO‑8601 round‑trip format (`"O"`) for the timestamp to ensure consistent parsing in logging systems.
