# AccountsController

The `AccountsController` is a controller in the `dotnet-cqrs-eventsourcing` project responsible for handling HTTP requests related to account management. It implements the Command Query Responsibility Segregation (CQRS) pattern, separating write operations (commands) like creating accounts, depositing, and withdrawing funds from read operations (queries) such as retrieving account details and event history. The controller interacts with an event-sourced system, where account state is reconstructed from a sequence of events, enabling auditability and replay capabilities.

## API

### `AccountsController`
**Purpose:** Initializes the controller with required dependencies for handling account-related operations.  
**Parameters:** Constructor parameters are not explicitly listed but typically include services for command handling, querying, and event storage.  
**Return Value:** N/A (constructor).  
**Exceptions:** May throw exceptions if required dependencies are not provided or misconfigured.

---

### `CreateAccount`
**Purpose:** Creates a new account by dispatching a `CreateAccountCommand`.  
**Parameters:**  
- `CreateAccountRequest request` - Contains account creation details (e.g., initial balance, account holder information).  
**Return Value:** `Task<IActionResult>` - Returns `201 Created` on success, `400 Bad Request` for invalid input.  
**Exceptions:** Throws `ArgumentException` if the request is null or contains invalid data.

---

### `GetAccountById`
**Purpose:** Retrieves account details by its unique identifier.  
**Parameters:**  
- `Guid id` - The unique identifier of the account.  
**Return Value:** `Task<IActionResult>` - Returns `200 OK` with account details, `404 Not Found` if the account does not exist.  
**Exceptions:** Throws `ArgumentNullException` if `id` is null.

---

### `Deposit`
**Purpose:** Deposits funds into an account by dispatching a `DepositCommand`.  
**Parameters:**  
- `TransactionRequest request` - Contains the account ID and deposit amount.  
**Return Value:** `Task<IActionResult>` - Returns `200 OK` on success, `400 Bad Request` for invalid input, `404 Not Found` if the account does not exist.  
**Exceptions:** Throws `InvalidOperationException` if the account is not in a valid state for deposits.

---

### `Withdraw`
**Purpose:** Withdraws funds from an account by dispatching a `WithdrawCommand`.  
**Parameters:**  
- `TransactionRequest request` - Contains the account ID and withdrawal amount.  
**Return Value:** `Task<IActionResult>` - Returns `200 OK` on success, `400 Bad Request` for invalid input, `404 Not Found` if the account does not exist, `409 Conflict` if insufficient funds.  
**Exceptions:** Throws `InvalidOperationException` if the account has insufficient funds or is not in a valid state.

---

### `GetAccountEvents`
**Purpose:** Retrieves the sequence of events associated with an account.  
**Parameters:**  
- `Guid id` - The unique identifier of the account.  
**Return Value:** `Task<IActionResult>` - Returns `200 OK` with a list of events, `404 Not Found` if the account does not exist.  
**Exceptions:** Throws `ArgumentNullException` if `id` is null.

---

### `ReplayAccountEvents`
**Purpose:** Replays all events for an account to reconstruct its current state.  
**Parameters:**  
- `Guid id` - The unique identifier of the account.  
**Return Value:** `Task<IActionResult>` - Returns `200 OK` on successful replay, `404 Not Found` if the account does not exist.  
**Exceptions:** Throws `ArgumentNullException` if `id` is null.

---

### `CreateAccountRequest`
**Purpose:** A record representing the data required to create a new account.  
**Properties:** Likely includes fields such as `InitialBalance` and `AccountHolderName`, though specifics are not provided.

---

### `TransactionRequest`
**Purpose:** A record representing the data required for a deposit or withdrawal transaction.  
**Properties:** Likely includes fields such as `AccountId` and `Amount`, though specifics are not provided.

## Usage

### Example 1: Creating an Account
```csharp
var client = new HttpClient();
var createRequest = new CreateAccountRequest { InitialBalance = 1000, AccountHolderName = "John Doe" };
var response = await client.PostAsJsonAsync("https://api.example.com/accounts", createRequest);
response.EnsureSuccessStatusCode();
Console.WriteLine($"Account created with status: {response.StatusCode}");
```

### Example 2: Depositing Funds
```csharp
var client = new HttpClient();
var transactionRequest = new TransactionRequest { AccountId = Guid.Parse("12345678-1234-1234-1234-123456789012"), Amount = 500 };
var response = await client.PostAsJsonAsync("https://api.example.com/accounts/deposit", transactionRequest);
response.EnsureSuccessStatusCode();
Console.WriteLine($"Deposit successful with status: {response.StatusCode}");
```

## Notes

- **Edge Cases:**  
  - Negative amounts in `TransactionRequest` may result in `400 Bad Request` responses.  
  - Concurrent modifications to an account (e.g., simultaneous deposits/withdrawals) may lead to optimistic concurrency conflicts if the underlying event store enforces versioning.  
  - Replaying events (`ReplayAccountEvents`) may be resource-intensive for accounts with extensive event histories and should be used cautiously in production environments.  

- **Thread Safety:**  
  - The controller itself is stateless and thread-safe, as each HTTP request is handled in isolation.  
  - Thread safety of underlying services (e.g., event store, command handlers) must be ensured by their implementations.  
  - Event replay operations may block or degrade performance if executed frequently due to I/O-bound event reconstruction.
