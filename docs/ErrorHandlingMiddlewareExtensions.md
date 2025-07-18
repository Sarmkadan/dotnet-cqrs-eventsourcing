# ErrorHandlingMiddlewareExtensions

The `ErrorHandlingMiddlewareExtensions` class provides a set of static extension methods designed to configure and enhance the behavior of the `ErrorHandlingMiddleware` within the `dotnet-cqrs-eventsourcing` pipeline. These methods facilitate the customization of error metadata, such as unique identifiers and timestamps, the injection of additional diagnostic details, and the standardized logging of exceptions, ensuring consistent error reporting and observability across the application.

## API

### `WithCustomErrorId`
Generates a new instance of `ErrorHandlingMiddleware` configured to assign a custom unique identifier to each processed error.
*   **Purpose**: Overrides the default error ID generation strategy to ensure traceability using a specific format or source.
*   **Parameters**: None.
*   **Return Value**: A new `ErrorHandlingMiddleware` instance with the custom ID logic applied.
*   **Throws**: No exceptions are thrown by this configuration method itself; exceptions may occur during the middleware's execution if the ID generation logic fails.

### `WithTimestamp`
Generates a new instance of `ErrorHandlingMiddleware` configured to attach a precise timestamp to error occurrences.
*   **Purpose**: Ensures that every error captured by the middleware includes a standardized time of occurrence, typically using UTC.
*   **Parameters**: None.
*   **Return Value**: A new `ErrorHandlingMiddleware` instance with timestamping enabled.
*   **Throws**: No exceptions are thrown by this configuration method.

### `LogError`
Executes the logging routine for a specific exception context.
*   **Purpose**: Writes error details, including message, stack trace, and any attached metadata, to the configured logging provider.
*   **Parameters**: Accepts the necessary context arguments required to resolve the exception and logging scope (specific signature details depend on the internal implementation, but logically requires an `Exception` object and potentially a correlation ID).
*   **Return Value**: `void`.
*   **Throws**: May throw exceptions if the underlying logging provider fails or if the input context is null/invalid.

### `AddDetail`
Generates a new instance of `ErrorHandlingMiddleware` configured to append specific key-value pairs to the error's metadata collection.
*   **Purpose**: Enriches error reports with contextual data (e.g., User ID, Tenant ID, Request Path) to aid in debugging and filtering.
*   **Parameters**: Typically accepts a key (string) and a value (object or string) representing the detail to add.
*   **Return Value**: A new `ErrorHandlingMiddleware` instance with the additional detail handler registered.
*   **Throws**: May throw `ArgumentException` if the provided key is null, empty, or duplicates an existing reserved key.

## Usage

The following example demonstrates chaining extension methods to create a highly configured middleware instance that assigns a custom GUID, records the UTC timestamp, and injects tenant information into every error.

```csharp
using DotNetCqrsEventSourcing.Middleware;

// Configure the middleware pipeline
var middleware = ErrorHandlingMiddleware.Create()
    .WithCustomErrorId()
    .WithTimestamp()
    .AddDetail("TenantId", tenantContext.Id);

// The configured middleware is now ready to be inserted into the request pipeline
app.UseMiddleware(middleware);
```

The next example illustrates the direct invocation of the logging helper within a catch block to ensure standardized error output before rethrowing or handling the failure.

```csharp
using DotNetCqrsEventSourcing.Middleware;
using Microsoft.Extensions.Logging;

public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(ILogger<OrderProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(Order order)
    {
        try
        {
            await ExecuteOrderLogic(order);
        }
        catch (Exception ex)
        {
            // Utilize the static helper to log with consistent formatting
            ErrorHandlingMiddleware.LogError(ex, _logger, orderId: order.Id);
            
            // Rethrow or handle as per business requirements
            throw;
        }
    }
}
```

## Notes

*   **Immutability**: Methods returning `ErrorHandlingMiddleware` (such as `WithCustomErrorId`, `WithTimestamp`, and `AddDetail`) follow an immutable pattern. They do not modify the existing instance but instead return a new configured instance. Failure to capture the return value will result in the configuration being lost.
*   **Thread Safety**: As these are static extension methods operating on configuration objects, the setup phase is generally thread-safe. However, the resulting middleware instance's runtime behavior (specifically `LogError`) depends on the thread safety of the underlying logging infrastructure and the state of the request context.
*   **Key Collisions**: When using `AddDetail`, care must be taken to avoid adding details with keys that conflict with system-reserved metadata keys (e.g., "ErrorId", "Timestamp"), which could lead to overwritten data or runtime exceptions depending on the strictness of the implementation.
*   **Execution Order**: The order in which these extensions are chained matters if multiple `AddDetail` calls are made with logic that depends on previous state, though typically they accumulate metadata additively.
