# AccountRepositoryExtensions

Extension methods for account repositories that encapsulate common account operations such as retrieval with fallback creation, status-based queries, conditional existence checks, and balance transfers. These methods return `Result<T>` monads to enforce explicit error handling and avoid exception-driven control flow.

## API

### GetOrCreateAsync

```csharp
public static async Task<Result<Account>> GetOrCreateAsync(
    this IAccountRepository repository,
    AccountId accountId,
    AccountFactory factory,
    CancellationToken cancellationToken = default)
```

Attempts to retrieve an account by its identifier. If the account does not exist, a new account is created using the provided factory and persisted to the repository. Returns the existing or newly created account wrapped in a success result, or a failure result if the repository operation fails or the factory throws.

**Parameters**
- `repository` — the account repository to query and persist against.
- `accountId` — the identifier of the account to retrieve or create.
- `factory` — a delegate or factory object responsible for constructing a new `Account` when one is not found.
- `cancellationToken` — optional cancellation token.

**Returns**
`Result<Account>` containing the account on success, or an error on failure.

**Throws**
No exceptions are thrown directly. All failures are captured in the returned `Result`.

---

### GetAllByStatusAsync

```csharp
public static async Task<Result<List<Account>>> GetAllByStatusAsync(
    this IAccountRepository repository,
    AccountStatus status,
    CancellationToken cancellationToken = default)
```

Retrieves all accounts matching the specified status. The underlying repository must support status-based filtering. An empty list is returned when no accounts match.

**Parameters**
- `repository` — the account repository to query.
- `status` — the `AccountStatus` enumeration value to filter by.
- `cancellationToken` — optional cancellation token.

**Returns**
`Result<List<Account>>` containing the matching accounts (possibly empty) on success, or an error on failure.

**Throws**
No exceptions are thrown directly. Repository-level failures are surfaced through the `Result`.

---

### GetIfExistsAsync

```csharp
public static async Task<Result<Account>> GetIfExistsAsync(
    this IAccountRepository repository,
    AccountId accountId,
    CancellationToken cancellationToken = default)
```

Retrieves an account by its identifier only if it already exists. Unlike `GetOrCreateAsync`, no fallback creation is performed. A failure result is returned when the account is not found.

**Parameters**
- `repository` — the account repository to query.
- `accountId` — the identifier of the account to retrieve.
- `cancellationToken` — optional cancellation token.

**Returns**
`Result<Account>` containing the account on success, or a failure result when the account does not exist or the repository operation fails.

**Throws**
No exceptions are thrown directly. The absence of an account is treated as a failure in the `Result`.

---

### TransferBalanceAsync

```csharp
public static async Task<Result> TransferBalanceAsync(
    this IAccountRepository repository,
    AccountId sourceAccountId,
    AccountId destinationAccountId,
    Money amount,
    CancellationToken cancellationToken = default)
```

Performs a balance transfer between two accounts. Both accounts must exist. The method validates that the source account has sufficient funds, debits the source, credits the destination, and persists both accounts atomically or within a transactional scope provided by the repository.

**Parameters**
- `repository` — the account repository to load and save accounts.
- `sourceAccountId` — the account from which funds are withdrawn.
- `destinationAccountId` — the account to which funds are deposited.
- `amount` — the monetary amount to transfer.
- `cancellationToken` — optional cancellation token.

**Returns**
`Result` indicating success or an error. Error scenarios include: either account not found, insufficient balance, or repository persistence failure.

**Throws**
No exceptions are thrown directly. All error conditions are returned as failure results.

## Usage

### Example 1: Retrieve or create an account, then query by status

```csharp
var accountId = new AccountId("ACC-123");
var factory = new DefaultAccountFactory();

// Get or create the account
var accountResult = await repository.GetOrCreateAsync(accountId, factory);
if (accountResult.IsFailure)
{
    _logger.LogError("Failed to get or create account: {Error}", accountResult.Error);
    return;
}

var account = accountResult.Value;

// Later, fetch all active accounts for a report
var activeAccountsResult = await repository.GetAllByStatusAsync(AccountStatus.Active);
if (activeAccountsResult.IsSuccess)
{
    foreach (var activeAccount in activeAccountsResult.Value)
    {
        Console.WriteLine($"Active account: {activeAccount.Id}");
    }
}
```

### Example 2: Conditional existence check followed by a balance transfer

```csharp
var sourceId = new AccountId("ACC-456");
var destinationId = new AccountId("ACC-789");
var transferAmount = new Money(250.00m, Currency.USD);

// Ensure the destination account exists before transferring
var destinationResult = await repository.GetIfExistsAsync(destinationId);
if (destinationResult.IsFailure)
{
    Console.WriteLine("Destination account does not exist. Transfer aborted.");
    return;
}

// Attempt the transfer
var transferResult = await repository.TransferBalanceAsync(sourceId, destinationId, transferAmount);
if (transferResult.IsFailure)
{
    Console.WriteLine($"Transfer failed: {transferResult.Error}");
    return;
}

Console.WriteLine("Transfer completed successfully.");
```

## Notes

- **Edge cases for `GetOrCreateAsync`**: Concurrent callers requesting the same non-existent account may trigger multiple factory invocations. The repository implementation is responsible for handling idempotency or unique constraint violations. The extension method itself does not perform locking or deduplication.
- **Edge cases for `GetIfExistsAsync`**: A failure result is returned for both "not found" and genuine repository errors. Callers should inspect the error details to distinguish between these cases if the distinction matters.
- **Edge cases for `TransferBalanceAsync`**: The method assumes the repository can perform the debit and credit atomically. If the repository lacks transaction support, partial updates may occur on failure. The method does not retry on transient failures; retry logic is the caller's responsibility.
- **Thread safety**: These extension methods are stateless and delegate all state management to the repository instance. Thread safety depends entirely on the thread safety guarantees of the underlying `IAccountRepository` implementation. No shared mutable state is introduced by the extensions.
- **Cancellation**: All methods accept a `CancellationToken`. If cancellation is requested before the repository operation completes, the underlying repository call is expected to throw an `OperationCanceledException`, which should be captured as a failure result by the extension method.
