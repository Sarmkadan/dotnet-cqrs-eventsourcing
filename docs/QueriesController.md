# QueriesController

The `QueriesController` is an ASP.NET Core controller responsible for handling read operations in a CQRS (Command Query Responsibility Segregation) and Event Sourcing architecture. It provides endpoints for querying account-related data, including balances, transaction histories, and statistics, while also supporting cache invalidation for query results.

## API

### `ListAccounts`
Retrieves a list of all accounts.

**Returns:**
- `IActionResult` containing a collection of account summaries.
- Returns `200 OK` with the account list on success.
- Returns `500 Internal Server Error` if an unexpected failure occurs during query execution.

---

### `SearchAccounts`
Searches accounts based on optional filtering criteria.

**Parameters:**
- `searchTerm` (string, optional): A term to filter accounts by name, identifier, or other searchable fields.
- `limit` (int, optional): Maximum number of results to return. Defaults to 100 if not specified.

**Returns:**
- `IActionResult` containing a filtered collection of account summaries.
- Returns `200 OK` with matching accounts on success.
- Returns `400 Bad Request` if the `limit` parameter is negative or exceeds a configured maximum.
- Returns `500 Internal Server Error` if an unexpected failure occurs during query execution.

---

### `GetAccountBalance`
Retrieves the current balance of a specified account.

**Parameters:**
- `accountId` (string, required): The unique identifier of the account.

**Returns:**
- `IActionResult` containing the account balance.
- Returns `200 OK` with the balance on success.
- Returns `404 Not Found` if the account does not exist.
- Returns `500 Internal Server Error` if an unexpected failure occurs during query execution.

---

### `GetTransactionHistory`
Retrieves the transaction history for a specified account.

**Parameters:**
- `accountId` (string, required): The unique identifier of the account.
- `skip` (int, optional): Number of transactions to skip (for pagination). Defaults to 0.
- `take` (int, optional): Number of transactions to return. Defaults to 50.

**Returns:**
- `IActionResult` containing a paginated list of transactions.
- Returns `200 OK` with the transaction history on success.
- Returns `400 Bad Request` if `skip` or `take` are negative or exceed configured limits.
- Returns `404 Not Found` if the account does not exist.
- Returns `500 Internal Server Error` if an unexpected failure occurs during query execution.

---

### `GetAccountStatistics`
Retrieves statistical data for a specified account, such as total transactions, average transaction value, or other derived metrics.

**Parameters:**
- `accountId` (string, required): The unique identifier of the account.
- `timeRange` (string, optional): A time range filter (e.g., "lastMonth", "lastYear"). Defaults to "allTime" if not specified.

**Returns:**
- `IActionResult` containing the account statistics.
- Returns `200 OK` with the statistics on success.
- Returns `400 Bad Request` if the `timeRange` is invalid.
- Returns `404 Not Found` if the account does not exist.
- Returns `500 Internal Server Error` if an unexpected failure occurs during query execution.

---

### `InvalidateQueryCache`
Invalidates cached query results for a specified account or globally.

**Parameters:**
- `accountId` (string, optional): The unique identifier of the account. If omitted, invalidates the entire query cache.

**Returns:**
- `IActionResult` indicating the outcome of the operation.
- Returns `200 OK` if the cache was successfully invalidated.
- Returns `500 Internal Server Error` if an unexpected failure occurs during cache invalidation.

## Usage

### Example 1: Retrieving Account Balance and Transaction History
