# AccountRepository

Centralizes CRUD operations for `Account` entities, providing asynchronous access to an underlying event-sourced store. Integrates with the CQRS pattern to ensure consistency and auditability of account state changes.

## API

### `AccountRepository`

Public constructor accepting an `IEventStore` instance used to load and persist account events.

### `async Task<Result<Account>> GetByIdAsync(Guid id)`

Retrieves the `Account` identified by `id` from the event store. Returns a `Result<Account>` containing the reconstructed account on success, or a failure reason if the account does not exist or the events cannot be loaded.

### `async Task<Result> SaveAsync(Account account)`

Persists all uncommitted events of `account` to the event store. Returns a `Result` indicating success or failure. Throws if `account` is null or its uncommitted events are invalid.

### `async Task<Result> DeleteAsync(Guid id)`

Marks the account identified by `id` as logically deleted by appending a deletion event to the event stream. Returns a `Result` indicating success or failure. Throws if `id` is empty.

### `async Task<Result<List<Account>>> GetAllAsync()`

Returns a `Result` containing a list of all accounts reconstructed from their event streams. The list is ordered by account creation time ascending. Throws if the event store fails to load any account stream.

### `async Task<bool> ExistsAsync(Guid id)`

Checks whether an account with the specified `id` exists by attempting to load its event stream. Returns `true` if the stream exists and is not marked as deleted, otherwise `false`. Throws if the event store is unavailable.

## Usage
