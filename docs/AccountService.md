# AccountService

The `AccountService` provides asynchronous operations for managing bank accounts in a CQRS-based event-sourcing system. It handles account creation, retrieval, deposits, withdrawals, closures, and transaction tracking while ensuring consistency through event sourcing and result-based responses.

## API

### `public AccountService`

Initializes a new instance of the `AccountService` with required dependencies for event sourcing and command handling.

### `public async Task<Result<Account>> CreateAccountAsync()`

Creates a new account with a unique identifier. The account is initialized with a zero balance and active status.

- **Returns**: A `Result<Account>` containing the newly created account or an error if the operation fails (e.g., due to duplicate account creation constraints).
- **Throws**: May throw exceptions from underlying infrastructure (e.g., database connectivity issues).

### `public async Task<Result<Account>> GetAccountAsync()`

Retrieves an account by its unique identifier.

- **Returns**: A `Result<Account>` containing the requested account or an error if the account does not exist or the operation fails.
- **Throws**: May throw exceptions from underlying infrastructure (e.g., network failures during query execution).

### `public async Task<Result> DepositAsync(Guid accountId, decimal amount)`

Deposits the specified amount into the account associated with `accountId`.

- **Parameters**:
  - `accountId`: The unique identifier of the target account.
  - `amount`: The positive decimal value to deposit.
- **Returns**: A `Result` indicating success or failure (e.g., invalid amount, account not found, or insufficient permissions).
- **Throws**: May throw exceptions from underlying infrastructure (e.g., event store write failures).

### `public async Task<Result> WithdrawAsync(Guid accountId, decimal amount)`

Withdraws the specified amount from the account associated with `accountId`.

- **Parameters**:
  - `accountId`: The unique identifier of the target account.
  - `amount`: The positive decimal value to withdraw.
- **Returns**: A `Result` indicating success or failure (e.g., invalid amount, account not found, insufficient funds, or insufficient permissions).
- **Throws**: May throw exceptions from underlying infrastructure (e.g., event store write failures).

### `public async Task<Result> CloseAccountAsync(Guid accountId)`

Closes the account associated with `accountId`, preventing further transactions.

- **Parameters**:
  - `accountId`: The unique identifier of the target account.
- **Returns**: A `Result` indicating success or failure (e.g., account not found, already closed, or pending transactions).
- **Throws**: May throw exceptions from underlying infrastructure (e.g., event store write failures).

### `public async Task<Result<List<Account>>> GetAllAccountsAsync()`

Retrieves all accounts in the system.

- **Returns**: A `Result<List<Account>>` containing the list of all accounts or an error if the operation fails.
- **Throws**: May throw exceptions from underlying infrastructure (e.g., database connectivity issues).

### `public async Task<Result<int>> GetTransactionCountAsync(Guid accountId)`

Retrieves the total number of transactions recorded for the account associated with `accountId`.

- **Parameters**:
  - `accountId`: The unique identifier of the target account.
- **Returns**: A `Result<int>` containing the transaction count or an error if the account does not exist or the operation fails.
- **Throws**: May throw exceptions from underlying infrastructure (e.g., query execution failures).

## Usage

### Example 1: Creating and querying an account
