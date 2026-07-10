# Account

Represents a bank account aggregate root in the event-sourced domain model. It encapsulates the account's identity, holder information, transactional balance, lifecycle status, and a chronological list of transactions. The type supports event replay to rebuild its state from a historical sequence of domain events.

## API

### Constructors

#### `Account()`
Parameterless constructor. Initializes a new account with default values. Intended primarily for deserialization or framework infrastructure; typically not used directly in application code.

#### `Account(string id)`
Creates a new account instance with the specified identifier.
- **Parameters**: `id` — the unique account identifier (account number).
- **Exceptions**: Throws `ArgumentNullException` if `id` is null. Throws `ArgumentException` if `id` is empty or whitespace.

### Properties

#### `string AccountNumber`
Gets the unique account number that identifies this account. Set during construction and immutable thereafter.

#### `string AccountHolder`
Gets or sets the name of the person or entity that holds the account.

#### `Balance Balance`
Gets the current balance of the account, represented as a `Balance` value object that encapsulates both amount and currency.

#### `AggregateStatus Status`
Gets the current lifecycle status of the account (e.g., Active, Closed). Managed internally through domain operations.

#### `List<Transaction> Transactions`
Gets the list of all transactions applied to this account, in chronological order. Each `Transaction` records an operation such as deposit, withdrawal, or account closure.

#### `DateTime OpenDate`
Gets the date and time when the account was opened. Set by `CreateAccount`.

#### `DateTime? CloseDate`
Gets the date and time when the account was closed, or null if the account remains open. Set by `CloseAccount`.

#### `long LastSnapshotVersion`
Gets the event stream version at which the last snapshot was taken. Used by the event store to optimize aggregate rehydration.

### Methods

#### `Account ReplayEvents`
Replays a sequence of domain events against the current instance, rebuilding its state incrementally. This method is central to the event-sourcing pattern, allowing the aggregate to be restored from its event history.
- **Returns**: The current `Account` instance with state rebuilt from the events.
- **Exceptions**: Throws `InvalidOperationException` if an event cannot be applied due to the account's current status (e.g., attempting to deposit to a closed account during replay).

#### `void CreateAccount`
Applies the account creation event, establishing the initial state including the account number, holder, opening date, and setting the status to Active.
- **Exceptions**: Throws `InvalidOperationException` if the account has already been created (status is not the initial default).

#### `void Deposit`
Applies a deposit transaction to the account, increasing the balance by the specified amount.
- **Parameters**: Implicitly expects amount and currency details via the command or event payload.
- **Exceptions**: Throws `InvalidOperationException` if the account is not in an Active status. Throws `ArgumentException` if the deposit amount is zero or negative.

#### `void Withdraw`
Applies a withdrawal transaction to the account, decreasing the balance by the specified amount.
- **Parameters**: Implicitly expects amount and currency details via the command or event payload.
- **Exceptions**: Throws `InvalidOperationException` if the account is not in an Active status. Throws `InvalidOperationException` if the withdrawal amount exceeds the current balance. Throws `ArgumentException` if the withdrawal amount is zero or negative.

#### `void CloseAccount`
Applies the account closure event, setting the status to Closed, recording the close date, and optionally processing a final balance settlement.
- **Exceptions**: Throws `InvalidOperationException` if the account is already closed.

#### `override string ToString()`
Returns a string representation of the account, typically including the account number, holder name, and current balance. Useful for logging and debugging.

## Usage

### Example 1: Creating an account and performing transactions

```csharp
// Create a new account
var account = new Account("ACC-20250317-001");
account.CreateAccount(holder: "Jane Doe", initialDeposit: 500.00m, currency: "USD");

// Perform a deposit
account.Deposit(amount: 200.00m, currency: "USD");

// Perform a withdrawal
account.Withdraw(amount: 150.00m, currency: "USD");

Console.WriteLine(account.ToString());
// Output: ACC-20250317-001 | Jane Doe | 550.00 USD | Active
```

### Example 2: Replaying events from an event store

```csharp
// Fetch events from the event store for a given aggregate ID
IEnumerable<IDomainEvent> eventStream = eventStore.LoadEvents("ACC-20250317-001");

// Rehydrate the account aggregate by replaying events
var account = new Account("ACC-20250317-001");
account.ReplayEvents(eventStream);

// Account state is now fully restored; it can accept new commands
if (account.Status == AggregateStatus.Active)
{
    account.Withdraw(amount: 50.00m, currency: "USD");
}
```

## Notes

- **Event replay idempotency**: `ReplayEvents` rebuilds state from scratch. Calling it on an aggregate that already has state will duplicate or conflict with existing data. Always replay against a freshly constructed instance or an instance loaded from a snapshot whose version matches `LastSnapshotVersion`.
- **Status guards**: `Deposit`, `Withdraw`, and `CloseAccount` all enforce that the account is Active before applying their respective events. Attempting these operations on a closed account throws `InvalidOperationException`.
- **Balance enforcement**: `Withdraw` throws if insufficient funds exist. The check is performed synchronously within the method; no overdraft is permitted by default.
- **Thread safety**: This aggregate is not thread-safe. Concurrent command execution against the same instance will lead to race conditions and inconsistent state. In an event-sourced system, consistency is typically enforced through single-threaded aggregate command handling or optimistic concurrency control at the event store level using the aggregate version.
- **Snapshot version**: `LastSnapshotVersion` is metadata for infrastructure concerns. It does not affect domain logic but is critical for optimizing aggregate loading when snapshots are used in conjunction with event replay.
