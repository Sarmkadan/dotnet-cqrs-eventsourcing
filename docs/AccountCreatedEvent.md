# AccountCreatedEvent

Represents an event that is raised when a new bank account is created in the system. It captures the essential details of the account at the moment of creation, including the account number, account holder name, currency, and the initial deposit amount. This event is immutable after creation and is typically stored in an event store as part of an event-sourced aggregate.

## API

### Properties

- **`AccountNumber`** (`string`)  
  The unique identifier assigned to the account. This value is set at construction and does not change.

- **`AccountHolder`** (`string`)  
  The name of the person or entity that owns the account.

- **`Currency`** (`string`)  
  The ISO 4217 currency code (e.g., "USD", "EUR") in which the account is denominated.

- **`InitialBalance`** (`decimal`)  
  The amount of money deposited into the account at the time of creation. This value must be non-negative.

- **`GetEventType`** (`string`)  
  An overridden property that returns a string identifying the type of the event. The exact value is determined by the base class implementation, but it is typically a constant such as `"AccountCreated"`. This property is used for serialization and event routing.

### Constructors

- **`AccountCreatedEvent()`**  
  Parameterless constructor intended for deserialization frameworks (e.g., JSON or binary serializers). All properties are initialized to their default values (`null` for strings, `0` for `InitialBalance`). After deserialization, properties should be set via the serializer.

- **`AccountCreatedEvent(string accountNumber, string accountHolder, string currency, decimal initialBalance)`**  
  Initializes a new instance with the specified account details.  
  **Parameters:**  
  - `accountNumber` – The unique account number.  
  - `accountHolder` – The name of the account holder.  
  - `currency` – The ISO currency code.  
  - `initialBalance` – The initial deposit amount.  
  **Exceptions:**  
  - `ArgumentNullException` – Thrown if `accountNumber`, `accountHolder`, or `currency` is `null`.  
  - `ArgumentException` – Thrown if `accountNumber`, `accountHolder`, or `currency` is empty or consists only of white-space characters.  
  - `ArgumentOutOfRangeException` – Thrown if `initialBalance` is negative.

- **`AccountCreatedEvent(AccountCreatedEvent other)`**  
  Copy constructor that creates a new instance with the same property values as the provided `other` event.  
  **Parameters:**  
  - `other` – The event to copy.  
  **Exceptions:**  
  - `ArgumentNullException` – Thrown if `other` is `null`.

## Usage

### Example 1: Creating and storing an event

```csharp
var createdEvent = new AccountCreatedEvent(
    accountNumber: "ACC-1001",
    accountHolder: "Jane Doe",
    currency: "USD",
    initialBalance: 500.00m
);

// The event can then be appended to an event store.
eventStore.Append(createdEvent);
```

### Example 2: Deserializing from a stored event

```csharp
string json = @"{""AccountNumber"":""ACC-1001"",""AccountHolder"":""Jane Doe"",""Currency"":""USD"",""InitialBalance"":500.00}";
var deserializedEvent = JsonSerializer.Deserialize<AccountCreatedEvent>(json);

Console.WriteLine(deserializedEvent.AccountNumber); // ACC-1001
Console.WriteLine(deserializedEvent.InitialBalance); // 500.00
```

## Notes

- **Immutability:** After construction, the property values of an `AccountCreatedEvent` instance should not be modified. The class does not expose public setters; all properties are read-only. The copy constructor creates a new instance with the same values, preserving immutability.
- **Thread safety:** Because the event is immutable, it is inherently thread-safe. Multiple threads can read the same instance without synchronization. The parameterless constructor is intended only for serialization and should not be used in business logic; instances created with it have default values that are not valid for a real account creation.
- **Edge cases:**  
  - `InitialBalance` can be zero, representing an account opened without an initial deposit.  
  - `AccountNumber`, `AccountHolder`, and `Currency` must not be `null`, empty, or whitespace. The constructors enforce this by throwing `ArgumentNullException` or `ArgumentException`.  
  - The `GetEventType` property is used by event sourcing infrastructure to route events to the correct handler. Its value is typically a constant and should not be relied upon for business logic.
- **Serialization:** The parameterless constructor exists to support common serializers (e.g., `System.Text.Json`, `Newtonsoft.Json`). When deserializing, ensure that the serializer can set the read-only properties (e.g., by using a constructor with parameters or by marking properties as `init`). The provided constructors cover both scenarios.
