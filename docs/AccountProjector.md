# AccountProjector

The `AccountProjector` class is responsible for transforming domain events or state into a read‑model representation of an `Account`. It exposes a simple contract that allows the hosting infrastructure to query whether the projector can handle a given context, obtain a projection key, and asynchronously produce the projected read model.

## API

### CanProject
```csharp
public bool CanProject { get; }
```
- **Purpose**: Indicates whether the projector is currently able to perform a projection.  
- **Return value**: `true` if the internal state is suitable for projection; otherwise `false`.  
- **Throws**: None. This property is a pure read‑only state check.

### GetKey
```csharp
public string GetKey { get; }
```
- **Purpose**: Provides a unique key that identifies the projection result for caching or storage purposes.  
- **Return value**: A non‑empty string key derived from the projector’s current context (e.g., account identifier).  
- **Throws**: None. The property is assumed to always return a valid key when `CanProject` is `true`; callers should verify `CanProject` before relying on the value.

### ProjectAsync
```csharp
public Task<AccountReadModel?> ProjectAsync();
```
- **Purpose**: Asynchronously projects the current state into an `AccountReadModel` instance.  
- **Parameters**: None. The projector uses its internal state to perform the projection.  
- **Return value**: A `Task` that completes with an `AccountReadModel` representing the projected account, or `null` if the projection cannot be produced (e.g., missing required data).  
- **Throws**:  
  - `InvalidOperationException` if the projector is not in a state that allows projection (`CanProject` is `false`).  
  - Any exception thrown by underlying dependencies (e.g., data access failures) is propagated unchanged.

## Usage

```csharp
// Example 1: Basic projection flow
var projector = new AccountProjector(/* dependencies */);
if (projector.CanProject)
{
    string key = projector.GetKey;          // e.g., "account-42"
    AccountReadModel? model = await projector.ProjectAsync();
    if (model != null)
    {
        // Store or return the read model
        await readModelRepository.SaveAsync(key, model);
    }
}
```

```csharp
// Example 2: Handling projection failure gracefully
var projector = new AccountProjector(/* dependencies */);
if (!projector.CanProject)
{
    // Log or raise a domain-specific event indicating the projector cannot run
    logger.Warning("Account projector cannot project at this time.");
    return null;
}

try
{
    AccountReadModel? model = await projector.ProjectAsync();
    return model ?? new AccountReadModel { IsEmpty = true };
}
catch (InvalidOperationException ex)
{
    // Unexpected state; treat as fatal for this operation
    logger.Error(ex, "Projection failed due to invalid projector state.");
    throw;
}
```

## Notes
- The projector is **not thread‑safe** by default. Concurrent calls to `CanProject`, `GetKey`, or `ProjectAsync` from multiple threads may result in inconsistent state. External synchronization (e.g., locking or ensuring single‑threaded access) is required when sharing an instance.
- `CanProject` must be checked before invoking `GetKey` or `ProjectAsync`; otherwise the returned key may be meaningless and the projection may throw.
- The `ProjectAsync` method may return `null` to signal that, while the projector is capable, the current data does not yield a meaningful read model (e.g., the account has not been initialized). Callers should handle the `null` case according to their application semantics.
- No state is mutated by `GetKey`; only `ProjectAsync` may read internal state, and any mutation of that state should occur outside the projector’s public interface.
