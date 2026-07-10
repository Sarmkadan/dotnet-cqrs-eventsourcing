# Balance

Represents the financial state of an account or entity within the event-sourced domain, tracking current, available, and held monetary amounts along with metadata about transaction activity. It is an immutable value type that evolves exclusively through domain events, ensuring a complete and auditable history of all balance changes.

## API

### Properties

#### `Money CurrentAmount`
The total amount of money currently recorded in the balance, including any funds placed on hold. This is the gross balance before subtracting holds.

#### `Money AvailableAmount`
The amount of money that is immediately available for withdrawal or spending. Computed as `CurrentAmount` minus `HoldAmount`.

#### `Money HoldAmount`
The total amount of money currently placed on hold, reserved for pending transactions or authorizations that have not yet been finalized.

#### `DateTime LastUpdated`
The timestamp of the most recent operation that modified the balance. Reflects the moment the last domain event was applied.

#### `int TransactionCount`
The cumulative number of transactions that have affected this balance over its lifetime. Increments with each successful fund addition, removal, hold placement, or hold release.

### Constructors

#### `Balance()`
Initializes a new instance of `Balance` with zero amounts, a transaction count of zero, and `LastUpdated` set to the current UTC time. Represents the starting state of a newly created account.

### Methods

#### `void AddFunds(Money amount)`
Applies a credit operation to the balance. Increases `CurrentAmount` and `AvailableAmount` by the specified amount, increments `TransactionCount`, and updates `LastUpdated`.

- **Parameters**: `amount` — the positive `Money` value to add.
- **Throws**: `ArgumentOutOfRangeException` if `amount` is zero or negative.

#### `void RemoveFunds(Money amount)`
Applies a debit operation to the balance. Decreases `CurrentAmount` and `AvailableAmount` by the specified amount, increments `TransactionCount`, and updates `LastUpdated`.

- **Parameters**: `amount` — the positive `Money` value to remove.
- **Throws**: `InvalidOperationException` if `amount` exceeds `AvailableAmount`.
- **Throws**: `ArgumentOutOfRangeException` if `amount` is zero or negative.

#### `void PlaceHold(Money amount)`
Reserves funds by moving the specified amount from the available balance to the held balance. `CurrentAmount` remains unchanged; `AvailableAmount` decreases and `HoldAmount` increases by the same amount. Increments `TransactionCount` and updates `LastUpdated`.

- **Parameters**: `amount` — the positive `Money` value to place on hold.
- **Throws**: `InvalidOperationException` if `amount` exceeds `AvailableAmount`.
- **Throws**: `ArgumentOutOfRangeException` if `amount` is zero or negative.

#### `void ReleaseHold(Money amount)`
Releases previously held funds back to the available balance. `CurrentAmount` remains unchanged; `AvailableAmount` increases and `HoldAmount` decreases by the specified amount. Increments `TransactionCount` and updates `LastUpdated`.

- **Parameters**: `amount` — the positive `Money` value to release from hold.
- **Throws**: `InvalidOperationException` if `amount` exceeds `HoldAmount`.
- **Throws**: `ArgumentOutOfRangeException` if `amount` is zero or negative.

#### `bool Equals(Balance other)`
Performs a value-based equality comparison with another `Balance` instance. Returns `true` if `CurrentAmount`, `AvailableAmount`, `HoldAmount`, `LastUpdated`, and `TransactionCount` are all equal.

- **Parameters**: `other` — the `Balance` to compare against.
- **Returns**: `true` if all fields match; otherwise `false`.

#### `override bool Equals(object obj)`
Performs a value-based equality comparison with an arbitrary object. Returns `true` only if `obj` is a `Balance` instance and all fields match.

- **Parameters**: `obj` — the object to compare against.
- **Returns**: `true` if `obj` is a `Balance` with identical field values; otherwise `false`.

#### `override int GetHashCode()`
Returns a hash code derived from all fields of the balance. Consistent with the overridden `Equals` implementation.

- **Returns**: An integer hash code for the current instance.

#### `override string ToString()`
Returns a string representation of the balance, typically including the current, available, and held amounts along with the transaction count and last updated timestamp.

- **Returns**: A formatted string describing the balance state.

## Usage

### Example 1: Basic Account Operations

```csharp
// Open a new account with a zero balance
var balance = new Balance();

// Deposit initial funds
var deposit = new Money(1000.00m, "USD");
balance.AddFunds(deposit);

// Place a hold for a pending card authorization
var authorization = new Money(150.00m, "USD");
balance.PlaceHold(authorization);

// Later, capture the authorization (release hold and remove funds)
balance.ReleaseHold(authorization);
balance.RemoveFunds(authorization);

Console.WriteLine(balance.ToString());
// Output shows CurrentAmount: 850.00 USD, AvailableAmount: 850.00 USD,
// HoldAmount: 0.00 USD, TransactionCount: 4
```

### Example 2: Rebuilding Balance from Event History

```csharp
public Balance RehydrateFromEvents(IEnumerable<IDomainEvent> events)
{
    var balance = new Balance();

    foreach (var domainEvent in events.OrderBy(e => e.OccurredAt))
    {
        switch (domainEvent)
        {
            case FundsDeposited deposited:
                balance.AddFunds(deposited.Amount);
                break;
            case FundsWithdrawn withdrawn:
                balance.RemoveFunds(withdrawn.Amount);
                break;
            case HoldPlaced holdPlaced:
                balance.PlaceHold(holdPlaced.Amount);
                break;
            case HoldReleased holdReleased:
                balance.ReleaseHold(holdReleased.Amount);
                break;
        }
    }

    return balance;
}
```

## Notes

- **Immutability pattern**: Each mutating method (`AddFunds`, `RemoveFunds`, `PlaceHold`, `ReleaseHold`) returns a new `Balance` instance with the updated state rather than modifying the existing instance in place. The original instance remains unchanged, preserving event-sourcing integrity.
- **Hold lifecycle**: Holds must be explicitly released before the corresponding funds can be removed. Attempting to remove funds that are still on hold will fail because `AvailableAmount` excludes held amounts.
- **Overdraft prevention**: `RemoveFunds` and `PlaceHold` both enforce that the requested amount does not exceed `AvailableAmount`. There is no implicit overdraft facility; any such behavior must be modeled as a separate domain concept.
- **Thread safety**: `Balance` is a value type designed for single-threaded reconstruction from event streams. It is not thread-safe for concurrent mutation. In multi-threaded scenarios, each thread should operate on its own copy or synchronize access externally.
- **Equality semantics**: Two `Balance` instances are considered equal only when all five fields match exactly, including `LastUpdated` down to the tick. This strict equality is intentional for event-sourced systems where timestamp precision matters for ordering and conflict detection.
- **Transaction count**: The counter increments on every successful mutating operation, including hold placements and releases. It serves as an optimistic concurrency control mechanism and a simple audit metric, not as a count of external business transactions.
