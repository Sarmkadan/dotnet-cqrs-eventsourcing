# Transaction

The `Transaction` record represents a single financial or domain event within the event-sourced aggregate stream. It captures the type, monetary amount, timestamp, external reference, optional description, and extensible metadata for a discrete operation. Instances are immutable and support value-based equality semantics.

## API

### Properties

- **`public string Id`**  
  The unique identifier for this transaction. Typically a GUID string assigned at creation. Used for deduplication and correlation across event streams.

- **`public TransactionType Type`**  
  The classification of the transaction (e.g., `Credit`, `Debit`, `Transfer`). Determines how the `Amount` is applied to balances and which business rules are enforced.

- **`public Money Amount`**  
  The monetary value of the transaction, represented as a `Money` value object (combining currency and amount). Must be a positive value; the `Type` property dictates whether it is an inflow or outflow.

- **`public DateTime TransactionDate`**  
  The UTC timestamp when the transaction occurred or was posted. Used for chronological ordering in event streams and balance calculations.

- **`public string Reference`**  
  An external reference code (e.g., payment gateway transaction ID, invoice number). Must be non-null and non-empty. Used for reconciliation with external systems.

- **`public string? Description`**  
  An optional human-readable note describing the transaction purpose or context. May be `null` when no description is available.

- **`public Dictionary<string, object> Metadata`**  
  A key-value store for arbitrary additional data (e.g., correlation IDs, tracing information, source system identifiers). The dictionary is never null but may be empty.

### Constructors

- **`public Transaction(string id, TransactionType type, Money amount, DateTime transactionDate, string reference, string? description, Dictionary<string, object> metadata)`**  
  Creates a new `Transaction` with all fields specified. Throws `ArgumentNullException` if `id`, `amount`, `reference`, or `metadata` is null. Throws `ArgumentException` if `id` or `reference` is empty or whitespace, or if `amount` is negative.

- **`public Transaction(string id, TransactionType type, Money amount, DateTime transactionDate, string reference)`**  
  Convenience constructor that sets `Description` to `null` and `Metadata` to an empty dictionary. Throws under the same conditions as the full constructor.

### Methods

- **`public bool Equals(Transaction? other)`**  
  Implements `IEquatable<Transaction>`. Returns `true` if the other instance is non-null and all properties (`Id`, `Type`, `Amount`, `TransactionDate`, `Reference`, `Description`, `Metadata`) are equal by value. `Metadata` equality uses dictionary key-value pair comparison.

- **`public override bool Equals(object? obj)`**  
  Delegates to the typed `Equals(Transaction?)` method after checking that `obj` is a `Transaction`. Returns `false` for null or non-`Transaction` arguments.

- **`public override int GetHashCode()`**  
  Computes a hash code from all property values. Consistent with the equality implementation; two equal `Transaction` instances produce the same hash code.

- **`public override string ToString()`**  
  Returns a string representation including the `Id`, `Type`, `Amount`, and `TransactionDate`. Suitable for logging and debugging.

## Usage

### Example 1: Creating and comparing transactions

```csharp
var metadata = new Dictionary<string, object>
{
    ["CorrelationId"] = Guid.NewGuid().ToString(),
    ["Source"] = "PaymentGateway"
};

var tx1 = new Transaction(
    id: Guid.NewGuid().ToString(),
    type: TransactionType.Credit,
    amount: new Money(150.00m, "EUR"),
    transactionDate: DateTime.UtcNow,
    reference: "PG-2025-001",
    description: "Customer deposit",
    metadata: metadata
);

var tx2 = tx1 with { Description = "Updated deposit" };

bool areEqual = tx1.Equals(tx2); // false — Description differs
```

### Example 2: Recording a debit with the convenience constructor

```csharp
var debitTx = new Transaction(
    id: Guid.NewGuid().ToString(),
    type: TransactionType.Debit,
    amount: new Money(75.50m, "USD"),
    transactionDate: DateTime.UtcNow,
    reference: "INV-98765"
);

Console.WriteLine(debitTx.ToString());
// Output: Transaction { Id = ..., Type = Debit, Amount = 75.50 USD, Date = ... }
```

## Notes

- **Immutability:** `Transaction` is a record type; all properties are init-only. Use `with` expressions to create modified copies rather than mutating existing instances.
- **Equality semantics:** Equality is structural and includes all properties. Two transactions with the same `Id` but differing metadata are considered distinct. This prevents accidental deduplication when metadata carries important context.
- **Metadata dictionary:** The `Metadata` dictionary is compared by key-value pairs during equality checks. Dictionary order does not affect equality. Modifying the dictionary after construction on a copied instance does not affect the original, but callers should avoid mutating shared dictionary references.
- **Thread safety:** Instances are immutable and safe to share across threads without synchronization. The `Metadata` dictionary is not frozen by default; if a reference to the original dictionary is retained externally and mutated, thread safety is compromised. Defensive copying of the dictionary at construction is recommended in multi-threaded scenarios.
- **Validation:** Constructors throw on null or invalid arguments at creation time. No subsequent method calls throw exceptions. `Equals` and `GetHashCode` handle null arguments gracefully.
- **`TransactionDate` precision:** The `DateTime` type includes `Kind`. Consumers should normalize to UTC before construction to avoid timezone-related ordering inconsistencies in event streams.
