# GetAccountQuery

`GetAccountQuery` represents a query object used to retrieve a single account by its identifier within the CQRS and event-sourcing architecture. It encapsulates the necessary parameters for the query, along with correlation and timestamp metadata for tracing and auditing purposes. The type is part of the `dotnet-cqrs-eventsourcing` project and follows the query-side pattern, separating read concerns from command processing.

## API

### Constructors

#### `GetAccountQuery()`
Parameterless constructor. Initializes a new instance of `GetAccountQuery` with default values. The `AccountId`, `CorrelationId`, and `IssuedAt` properties must be set separately after construction if they are required for the query.

#### `GetAccountQuery(string accountId, string correlationId, DateTime issuedAt)`
Constructs a fully initialized `GetAccountQuery`.

| Parameter       | Type       | Description                                                                 |
|-----------------|------------|-----------------------------------------------------------------------------|
| `accountId`     | `string`   | The unique identifier of the account to retrieve.                           |
| `correlationId` | `string`   | A correlation identifier used to link this query to a broader workflow or request chain. |
| `issuedAt`      | `DateTime` | The timestamp indicating when the query was issued.                         |

**Return value:** A new `GetAccountQuery` instance with all properties populated.

**Exceptions:** None thrown directly by the constructor.

### Properties

#### `AccountId` : `string`
Gets or sets the unique identifier of the account targeted by this query. This value determines which account aggregate is read from the event store or read model.

#### `CorrelationId` : `string`
Gets or sets the correlation identifier. This is typically propagated from an incoming request or message and allows tracking of the query across distributed components and logs.

#### `IssuedAt` : `DateTime`
Gets or sets the UTC (or local, depending on system convention) timestamp at which the query was created. Used for diagnostics, latency measurement, and auditing.

### Methods

#### `override string ToString()`
Returns a string representation of the query, typically including the `AccountId` and possibly the `CorrelationId` and `IssuedAt` values for debugging and logging purposes.

**Return value:** A formatted string containing key property values.

**Exceptions:** None thrown.

## Usage

### Example 1: Dispatching a query for a specific account

```csharp
// In a controller or application service
var query = new GetAccountQuery(
    accountId: "acc-9f8e7d6c",
    correlationId: Activity.Current?.Id ?? Guid.NewGuid().ToString(),
    issuedAt: DateTime.UtcNow
);

var account = await _mediator.Send(query);

if (account == null)
{
    // Handle account not found
    return NotFound();
}

return Ok(account);
```

### Example 2: Building a query manually and logging its details

```csharp
var query = new GetAccountQuery();
query.AccountId = "acc-12345";
query.CorrelationId = "req-67890";
query.IssuedAt = DateTime.UtcNow;

_logger.LogInformation("Executing {QueryType} for {AccountId} [Correlation: {CorrelationId}]",
    nameof(GetAccountQuery),
    query.AccountId,
    query.CorrelationId);

var result = await _queryHandler.Handle(query);

// Log the result for diagnostics
_logger.LogDebug("Query result: {Query}", query.ToString());
```

## Notes

- **Missing AccountId:** If `AccountId` is null or empty, the query handler is expected to reject the query or return a null/empty result. The query object itself does not enforce validation; this responsibility lies with the handler or a validation pipeline.
- **CorrelationId propagation:** The `CorrelationId` should be carried from the originating request or message. If not set, tracing across services becomes fragmented. Consider using `Activity.Current?.Id` or a similar ambient context to populate it automatically.
- **IssuedAt precision:** `DateTime` precision depends on the system clock. For high-resolution timing, consider using `DateTime.UtcNow` consistently and avoid mixing local and UTC times.
- **Thread safety:** `GetAccountQuery` is a plain data object with public get/set properties. It is not inherently thread-safe. Instances should be treated as immutable once dispatched, or synchronized externally if shared across threads.
- **ToString format:** The exact format of `ToString()` is implementation-specific and should not be parsed programmatically. It is intended for human-readable diagnostics only.
- **Related queries:** The project also defines `GetAllAccountsQuery` (with `PageNumber`, `PageSize`, `CorrelationId`, `IssuedAt`) for paged retrieval of multiple accounts, and `GetTransactionCountQuery` (with `AccountId`, `CorrelationId`, `IssuedAt`) for counting transactions associated with an account. These share the same metadata pattern (`CorrelationId`, `IssuedAt`) for consistency across the query layer.
