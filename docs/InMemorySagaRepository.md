# InMemorySagaRepository

`InMemorySagaRepository` is an in-memory implementation of a saga persistence mechanism for the `dotnet-cqrs-eventsourcing` framework. It stores saga instances in a thread-safe concurrent dictionary, providing a lightweight repository suitable for testing, prototyping, or scenarios where durable saga storage is not required. All operations return `Result` objects that encapsulate success or failure outcomes without throwing exceptions for domain-level misses.

## API

### `GetByIdAsync`

Retrieves a saga instance by its unique identifier.

**Signature:**
```csharp
public Task<Result<TSaga>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

**Parameters:**
- `id` (`string`): The unique identifier of the saga.
- `cancellationToken` (`CancellationToken`): Optional cancellation token.

**Returns:**
`Task<Result<TSaga>>` — A successful result containing the saga instance if found; a failure result if the saga does not exist in the store.

**Throws:**
- `ArgumentNullException` when `id` is `null`.

---

### `FindByCorrelationIdAsync`

Locates a saga instance using a correlation identifier, typically used to associate sagas with external business processes or messages.

**Signature:**
```csharp
public Task<Result<TSaga>> FindByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
```

**Parameters:**
- `correlationId` (`string`): The correlation identifier to search for.
- `cancellationToken` (`CancellationToken`): Optional cancellation token.

**Returns:**
`Task<Result<TSaga>>` — A successful result containing the saga instance if exactly one match is found; a failure result if no saga matches or if multiple sagas share the same correlation identifier.

**Throws:**
- `ArgumentNullException` when `correlationId` is `null`.

---

### `SaveAsync`

Persists a saga instance. If a saga with the same identifier already exists, it is overwritten.

**Signature:**
```csharp
public Task<Result> SaveAsync(TSaga saga, CancellationToken cancellationToken = default)
```

**Parameters:**
- `saga` (`TSaga`): The saga instance to store.
- `cancellationToken` (`CancellationToken`): Optional cancellation token.

**Returns:**
`Task<Result>` — A successful result when the saga has been stored; a failure result if the operation cannot be completed (e.g., the saga argument is `null`).

**Throws:**
- `ArgumentNullException` when `saga` is `null`.

---

### `GetAllAsync`

Returns all saga instances currently held in the repository.

**Signature:**
```csharp
public Task<Result<IReadOnlyList<TSaga>>> GetAllAsync(CancellationToken cancellationToken = default)
```

**Parameters:**
- `cancellationToken` (`CancellationToken`): Optional cancellation token.

**Returns:**
`Task<Result<IReadOnlyList<TSaga>>>` — A successful result containing a read-only list of all stored sagas. The list may be empty if no sagas have been saved.

**Throws:**
No documented exceptions beyond those propagated by the underlying task infrastructure.

---

### `DeleteAsync`

Removes a saga instance from the repository by its unique identifier.

**Signature:**
```csharp
public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default)
```

**Parameters:**
- `id` (`string`): The unique identifier of the saga to remove.
- `cancellationToken` (`CancellationToken`): Optional cancellation token.

**Returns:**
`Task<Result>` — A successful result when the saga has been removed; a failure result if no saga with the given identifier exists.

**Throws:**
- `ArgumentNullException` when `id` is `null`.

---

## Usage

### Example 1: Creating, saving, and retrieving a saga

```csharp
var repository = new InMemorySagaRepository<OrderSaga>();
var saga = new OrderSaga
{
    Id = Guid.NewGuid().ToString(),
    CorrelationId = "order-12345",
    Status = SagaStatus.Pending
};

Result saveResult = await repository.SaveAsync(saga);
if (saveResult.IsSuccess)
{
    Result<OrderSaga> getResult = await repository.GetByIdAsync(saga.Id);
    if (getResult.IsSuccess)
    {
        OrderSaga retrieved = getResult.Value;
        Console.WriteLine($"Retrieved saga with status: {retrieved.Status}");
    }
}
```

### Example 2: Locating a saga by correlation identifier and deleting it

```csharp
var repository = new InMemorySagaRepository<OrderSaga>();

Result<OrderSaga> findResult = await repository.FindByCorrelationIdAsync("order-12345");
if (findResult.IsSuccess)
{
    OrderSaga saga = findResult.Value;
    Result deleteResult = await repository.DeleteAsync(saga.Id);
    if (deleteResult.IsSuccess)
    {
        Console.WriteLine("Saga successfully removed.");
    }
}
else
{
    Console.WriteLine("Saga not found or duplicate correlation IDs exist.");
}
```

---

## Notes

- **Thread safety:** The underlying store uses a concurrent dictionary, making individual operations atomic and safe for concurrent access. However, compound operations (e.g., check-then-save) are not transactional and may be subject to race conditions when multiple threads act on the same saga identifier.
- **Duplicate correlation IDs:** `FindByCorrelationIdAsync` returns a failure result if more than one saga shares the same correlation identifier. Callers must ensure correlation IDs remain unique if this lookup method is used.
- **Overwrite behavior:** `SaveAsync` unconditionally overwrites any existing saga with the same identifier. No concurrency token or version check is performed.
- **In-memory lifetime:** All data is lost when the repository instance is garbage collected or the process terminates. This implementation is not suitable for production durability requirements.
- **Null handling:** Methods throw `ArgumentNullException` for required `string` arguments and saga references. These exceptions are thrown synchronously before any task is returned.
