# RequestContextMiddleware

The `RequestContextMiddleware` component enriches the ASP.NET Core pipeline with request‑scoped metadata such as correlation identifiers and request details. It populates a static ambient context that can be accessed anywhere during the logical execution of a request, enabling consistent tracing, logging, and user‑information propagation without passing parameters explicitly.

## API

### RequestContextMiddleware
**Purpose:** Creates a new instance of the middleware.  
**Parameters:** None (parameterless constructor).  
**Return Value:** A new `RequestContextMiddleware` object.  
**Throws:** None.

### InvokeAsync
**Purpose:** Processes the incoming HTTP request, extracts request‑specific data, and stores it in the static request context for the duration of the request.  
**Parameters:** None (the method is called by the ASP.NET Core pipeline).  
**Return Value:** A `Task` that completes when the request has been processed.  
**Throws:**  
- `InvalidOperationException` if the method is invoked outside of an ASP.NET Core HTTP pipeline (e.g., called directly without an active `HttpContext`).

### CorrelationId
**Purpose:** Retrieves the correlation identifier associated with the current request.  
**Parameters:** None.  
**Return Value:** The correlation ID as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context has been established for the current call scope.

### RequestId
**Purpose:** Retrieves the unique request identifier for the current request.  
**Parameters:** None.  
**Return Value:** The request ID as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context has been established for the current call scope.

### UserId
**Purpose:** Retrieves the identifier of the user associated with the current request, if available.  
**Parameters:** None.  
**Return Value:** The user ID as a `string?`; returns `null` when the request is unauthenticated or no user information is present.  
**Throws:** None.

### Timestamp
**Purpose:** Retrieves the UTC timestamp indicating when the request began processing.  
**Parameters:** None.  
**Return Value:** The timestamp as a `DateTime`.  
**Throws:**  
- `InvalidOperationException` if no request context has been established for the current call scope.

### Path
**Purpose:** Retrieves the request path (the URL path component) of the current request.  
**Parameters:** None.  
**Return Value:** The path as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context has been established for the current call scope.

### Method
**Purpose:** Retrieves the HTTP method (e.g., GET, POST) of the current request.  
**Parameters:** None.  
**Return Value:** The method as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context has been established for the current call scope.

### SetContext
**Purpose:** Assigns the request‑specific values contained in the current `RequestContextMiddleware` instance to the ambient static context, making them accessible via the static getter methods for the remainder of the logical call sequence.  
**Parameters:** None (the instance on which the method is called provides the values).  
**Return Value:** `void`.  
**Throws:**  
- `InvalidOperationException` if called outside of a request scope (i.e., when no ambient context slot is available).

### GetContext
**Purpose:** Obtains the complete set of request‑scoped information stored in the ambient context.  
**Parameters:** None.  
**Return Value:** An instance of `RequestContextInfo` containing the current request data, or `null` if no context has been set.  
**Throws:** None.

### GetCorrelationId
**Purpose:** Returns the correlation identifier from the current ambient request context.  
**Parameters:** None.  
**Return Value:** The correlation ID as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context is present.

### GetRequestId
**Purpose:** Returns the request identifier from the current ambient request context.  
**Parameters:** None.  
**Return Value:** The request ID as a `string`.  
**Throws:**  
- `InvalidOperationException` if no request context is present.

### GetUserId
**Purpose:** Returns the user identifier from the current ambient request context, if any.  
**Parameters:** None.  
**Return Value:** The user ID as a `string?`; may be `null`.  
**Throws:** None.

### Clear
**Purpose:** Removes any request‑scoped data stored in the ambient context for the current logical call sequence.  
**Parameters:** None.  
**Return Value:** `void`.  
**Throws:** None.

### UseRequestContext
**Purpose:** Extension method that registers the `RequestContextMiddleware` component in the ASP.NET Core application’s request processing pipeline.  
**Parameters:** The method is invoked on an `IApplicationBuilder` instance (the builder supplied by the host).  
**Return Value:** The same `IApplicationBuilder` instance to allow fluent chaining.  
**Throws:**  
- `ArgumentNullException` if the builder instance supplied to the extension method is `null`.

## Usage

### Adding the middleware to the pipeline
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register any required services (if needed)
builder.Services.AddControllers();

var app = builder.Build();

// Insert the RequestContextMiddleware early in the pipeline
app.UseRequestContext();

app.MapControllers();

app.Run();
```

### Accessing request‑scoped information from a service or controller
```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TraceController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Retrieve correlation ID for logging or tracing
        var correlationId = RequestContextMiddleware.GetCorrelationId();

        // Optionally fetch the full context
        var ctx = RequestContextMiddleware.GetContext();
        var userId = ctx?.UserId;
        var timestamp = ctx?.Timestamp;

        return Ok(new
        {
            CorrelationId = correlationId,
            UserId = userId,
            Timestamp = timestamp?.ToString("o")
        });
    }
}
```

## Notes
- The static context is implemented using `AsyncLocal<T>`‑style storage, which ensures that each asynchronous logical call flow (i.e., each HTTP request) receives its own isolated set of values. Consequently, the middleware is thread‑safe for concurrent requests.
- If any of the getter properties (`CorrelationId`, `RequestId`, `UserId`, `Timestamp`, `Path`, `Method`) are accessed outside of a request scope (for example, during application startup or in a background thread that never entered the middleware), they will throw an `InvalidOperationException`. Callers should guard against this by checking `GetContext()` for `null` before accessing individual values when the call context is uncertain.
- Manually invoking `SetContext` bypasses the middleware’s automatic population of values and can lead to inconsistent state if not all relevant properties are set on the instance prior to the call. It is generally recommended to rely on the middleware’s `InvokeAsync` to establish the context.
- Calling `Clear` removes the context for the remainder of the current logical call sequence. After `Clear` is invoked, subsequent getter calls will behave as if no context had been set (throwing `InvalidOperationException` or returning `null` where applicable). This can be useful in scenarios where a request spawns detached background work that should not inherit the original request’s identifiers.
- The `UseRequestContext` extension method must be called before any middleware that depends on the request context (e.g., custom logging, authentication, or health‑check components) to ensure the values are available when those components execute. Placing it after terminal middleware (such as `UseRouting` or `UseEndpoints`) will result in the context never being populated for downstream components.
