// existing content ...

## IIdempotencyKeyHandler

The `IIdempotencyKeyHandler` interface ensures idempotency for operations by storing and retrieving results associated with unique keys. It prevents duplicate processing of identical requests using in-memory storage (via `InMemoryIdempotencyKeyHandler`), with configurable retention periods and automatic cleanup. It provides methods to check for existing results, record new ones, and clear stored keys.

### Usage Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIdempotency(); // Registers IIdempotencyKeyHandler
    }

    public async Task ExampleUsage()
    {
        var handler = new InMemoryIdempotencyKeyHandler(logger, TimeSpan.FromHours(24));
        
        // Record a result for a key
        var result = new IdempotencyResult
        {
            StatusCode = 200,
            ResponseBody = "{\"message\": \"success\"}",
            RecordedAt = DateTime.UtcNow
        };
        await handler.RecordResultAsync("unique-key-123", result);

        // Retrieve the result later
        var previousResult = await handler.GetPreviousResultAsync("unique-key-123");
        if (previousResult is not null)
        {
            Console.WriteLine($"Cached response: {previousResult.Value.ResponseBody}");
        }

        // Clear all stored keys (e.g., for testing)
        await handler.ClearAsync();
    }
}
```

This example demonstrates:
1. Registering the idempotency service via `AddIdempotency()`
2. Using `InMemoryIdempotencyKeyHandler` to store and retrieve results
3. The structure of `IdempotencyResult` with its required properties
4. Configuring retention periods and cleanup

The middleware is automatically enabled via `UseIdempotency()` in the request pipeline to enforce idempotency for POST/PUT/DELETE/PATCH requests with `Idempotency-Key` headers.
