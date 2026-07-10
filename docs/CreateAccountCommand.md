# CreateAccountCommand

Represents a command to create a new bank account with an initial balance in a specific currency. This command is part of the CQRS and event sourcing pattern implementation, where commands trigger state changes that are persisted as events.

## API

### `public string AccountNumber`
The unique identifier for the account being created. This value must be unique across all accounts in the system.

### `public string AccountHolder`
The name of the account holder as registered in the system.

### `public string Currency`
The ISO currency code (e.g., "USD", "EUR") for the account's balance.

### `public decimal InitialBalance`
The starting balance for the newly created account. Must be a non-negative value.

### `public string CorrelationId`
A unique identifier used to correlate this command with related events, logs, or other operations in the system.

### `public DateTime IssuedAt`
The timestamp indicating when the command was issued. Used for ordering and auditing purposes.

### `public CreateAccountCommand(string accountNumber, string accountHolder, string currency, decimal initialBalance, string correlationId, DateTime issuedAt)`
Constructs a new `CreateAccountCommand` with the specified parameters.

- **Parameters**:
  - `accountNumber`: The unique account identifier.
  - `accountHolder`: The name of the account holder.
  - `currency`: The ISO currency code.
  - `initialBalance`: The starting balance (must be ≥ 0).
  - `correlationId`: A unique correlation identifier.
  - `issuedAt`: The timestamp of command issuance.
- **Throws**: `ArgumentException` if `initialBalance` is negative or if any required parameter is null or whitespace.

### `public override string ToString()`
Returns a string representation of the command for debugging and logging purposes.

- **Returns**: A formatted string containing the command's properties and values.

## Usage

### Example 1: Creating a new account
