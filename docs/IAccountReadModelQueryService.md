# IAccountReadModelQueryService

Defines the query contract for retrieving account read models and portfolio statistics from the projection store. This interface abstracts the read side of the CQRS pattern, providing methods to fetch denormalized account state by identifier, account number, holder, status, or balance criteria without coupling consumers to the underlying storage implementation.

## API

### AccountPortfolioStatistics

```
public sealed record AccountPortfolioStatistics
```

An immutable data transfer object representing aggregated statistics across the account portfolio. Contains summary metrics such as total accounts, aggregate balances, and distribution data computed by the `GetPortfolioStatisticsAsync` query.

### AccountReadModelQueryService

```
public AccountReadModelQueryService
```

The concrete implementation class for `IAccountReadModelQueryService`. Constructed with dependencies on the read model projection store and any required materialized views. Not part of the interface contract itself but listed here as the canonical implementation type.

### GetByIdAsync

```csharp
public async Task<Result<AccountReadModel>> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
```

Retrieves a single account read model by its unique aggregate identifier.

**Parameters:**
- `accountId` — The `Guid` that uniquely identifies the account aggregate.
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<AccountReadModel>` — On success, contains the fully projected account read model. On failure, contains an error describing why the account could not be retrieved (e.g., not found, projection not yet materialized).

**Throws:**
- No exceptions are thrown directly. All failures are encapsulated in the `Result` error channel.

### GetByAccountNumberAsync

```csharp
public async Task<Result<AccountReadModel>> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
```

Retrieves a single account read model by its business account number.

**Parameters:**
- `accountNumber` — The human-readable account number string assigned to the account.
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<AccountReadModel>` — On success, contains the account read model matching the given account number. Returns a failure result if no account exists with that number or the projection is stale.

**Throws:**
- No exceptions are thrown directly. Errors are surfaced through the `Result` type.

### GetActiveAccountsAsync

```csharp
public Task<Result<IReadOnlyList<AccountReadModel>>> GetActiveAccountsAsync(CancellationToken cancellationToken = default)
```

Returns all accounts currently marked as active in the projection store.

**Parameters:**
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<IReadOnlyList<AccountReadModel>>` — On success, a read-only list of active account read models. The list may be empty if no active accounts exist. On failure, an error describing the query failure.

**Throws:**
- No exceptions are thrown directly.

### GetByAccountHolderAsync

```csharp
public Task<Result<IReadOnlyList<AccountReadModel>>> GetByAccountHolderAsync(string accountHolder, CancellationToken cancellationToken = default)
```

Retrieves all accounts associated with a given account holder name.

**Parameters:**
- `accountHolder` — The name of the account holder to filter by. Matching semantics (exact, case-insensitive, etc.) are implementation-defined.
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<IReadOnlyList<AccountReadModel>>` — On success, a read-only list of accounts belonging to the specified holder. Returns an empty list if the holder has no accounts. On failure, an error describing the query failure.

**Throws:**
- No exceptions are thrown directly.

### GetTopBalanceAccountsAsync

```csharp
public async Task<Result<IReadOnlyList<AccountReadModel>>> GetTopBalanceAccountsAsync(int count, CancellationToken cancellationToken = default)
```

Retrieves a specified number of accounts with the highest balances, ordered descending.

**Parameters:**
- `count` — The maximum number of top-balance accounts to return. Must be greater than zero.
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<IReadOnlyList<AccountReadModel>>` — On success, a read-only list of up to `count` accounts sorted by balance in descending order. May contain fewer items than requested if the total account count is smaller. On failure, an error describing the query failure.

**Throws:**
- No exceptions are thrown directly. Invalid `count` values (zero or negative) produce a failure `Result`.

### GetByBalanceRangeAsync

```csharp
public Task<Result<IReadOnlyList<AccountReadModel>>> GetByBalanceRangeAsync(decimal minBalance, decimal maxBalance, CancellationToken cancellationToken = default)
```

Retrieves all accounts whose current balance falls within the inclusive range `[minBalance, maxBalance]`.

**Parameters:**
- `minBalance` — The lower bound of the balance range (inclusive).
- `maxBalance` — The upper bound of the balance range (inclusive). Must be greater than or equal to `minBalance`.
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<IReadOnlyList<AccountReadModel>>` — On success, a read-only list of accounts within the specified balance range, sorted in an implementation-defined order. Returns an empty list if no accounts fall within the range. On failure, an error describing the query failure.

**Throws:**
- No exceptions are thrown directly. An inverted range (`minBalance > maxBalance`) produces a failure `Result`.

### GetPortfolioStatisticsAsync

```csharp
public async Task<Result<AccountPortfolioStatistics>> GetPortfolioStatisticsAsync(CancellationToken cancellationToken = default)
```

Computes and returns aggregate portfolio-wide statistics from the current projection state.

**Parameters:**
- `cancellationToken` — Optional cancellation token.

**Returns:**
- `Result<AccountPortfolioStatistics>` — On success, an `AccountPortfolioStatistics` record containing computed metrics such as total account count, sum of all balances, average balance, and any distribution breakdowns. On failure, an error describing why the aggregation could not be performed.

**Throws:**
- No exceptions are thrown directly.

## Usage

### Example 1: Retrieving an account by number and checking its status

```csharp
IAccountReadModelQueryService queryService = new AccountReadModelQueryService(readModelStore);

string targetAccountNumber = "ACC-2024-00142";
Result<AccountReadModel> result = await queryService.GetByAccountNumberAsync(targetAccountNumber);

if (result.IsSuccess)
{
    AccountReadModel account = result.Value;
    Console.WriteLine($"Account {account.AccountNumber} balance: {account.Balance:C}");
}
else
{
    Console.WriteLine($"Failed to retrieve account: {result.Error.Message}");
}
```

### Example 2: Fetching top accounts and portfolio statistics for a dashboard

```csharp
IAccountReadModelQueryService queryService = new AccountReadModelQueryService(readModelStore);
CancellationToken ct = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

// Retrieve top 10 accounts by balance
Result<IReadOnlyList<AccountReadModel>> topAccountsResult = await queryService.GetTopBalanceAccountsAsync(10, ct);

// Retrieve aggregate portfolio statistics
Result<AccountPortfolioStatistics> statsResult = await queryService.GetPortfolioStatisticsAsync(ct);

if (topAccountsResult.IsSuccess && statsResult.IsSuccess)
{
    IReadOnlyList<AccountReadModel> topAccounts = topAccountsResult.Value;
    AccountPortfolioStatistics stats = statsResult.Value;

    Console.WriteLine($"Total accounts: {stats.TotalAccounts}");
    Console.WriteLine("Top accounts:");
    foreach (var account in topAccounts)
    {
        Console.WriteLine($"  {account.AccountHolder}: {account.Balance:C}");
    }
}
else
{
    Console.WriteLine("Dashboard data unavailable. One or more queries failed.");
}
```

## Notes

- **Eventual consistency:** All query methods read from the projection store, which is updated asynchronously in response to domain events. Results may lag behind the latest committed events. Callers should not assume immediate read-your-writes consistency.
- **Empty results vs. failures:** An empty list returned in a successful `Result` indicates that no records matched the criteria. A failed `Result` indicates a query execution error (e.g., projection store unavailable, timeout). Consumers must distinguish between these two cases.
- **Cancellation:** All methods accept an optional `CancellationToken`. When cancelled, the returned `Result` will contain a failure with an error indicating the operation was cancelled. The underlying implementation is expected to honor cancellation requests cooperatively.
- **Thread safety:** The interface itself is stateless. The concrete `AccountReadModelQueryService` implementation is expected to be thread-safe, relying on the thread safety guarantees of the underlying projection store client. Multiple concurrent queries can be issued safely.
- **Parameter validation:** Methods with range or count parameters (`GetTopBalanceAccountsAsync`, `GetByBalanceRangeAsync`) validate arguments and return failure results for invalid inputs rather than throwing exceptions. Callers should inspect the `Result` for such validation errors.
- **`AccountPortfolioStatistics` immutability:** The returned statistics record is sealed and immutable. Its values represent a point-in-time snapshot and should not be cached beyond the desired freshness window.
