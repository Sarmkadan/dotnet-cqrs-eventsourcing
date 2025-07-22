# RequestContextMiddlewareExtensions

Provides extension methods for registering and consuming request-scoped context data in an ASP.NET Core pipeline. The middleware captures metadata (correlation ID, request ID, user ID, timestamp, HTTP method, and path) from each incoming HTTP request and stores it in a `RequestContextInfo` object accessible throughout the request lifetime. The static helper methods allow downstream code to retrieve this information without coupling to the middleware implementation.

## API

### `UseRequestContext(this IApplicationBuilder app)`

Registers the request context middleware in the ASP.NET Core pipeline. This overload uses default configuration (e.g., auto-generating a correlation ID if none is present in the request headers).

- **Parameters**: `app` – The `IApplicationBuilder` to extend.
- **Returns**: The same `IApplicationBuilder` instance for chaining.
- **Throws**: `ArgumentNullException` if `app` is `null`.

### `UseRequestContext(this IApplicationBuilder app, Action<RequestContextOptions> configureOptions)`

Registers the request context middleware with custom options, such as specifying the header name for the correlation ID or enabling/disabling automatic generation.

- **Parameters**:
  - `app` – The `IApplicationBuilder` to extend.
  - `configureOptions` – A delegate to configure `RequestContextOptions`.
- **Returns**: The same `IApplicationBuilder` instance for chaining.
- **Throws**: `ArgumentNullException` if `app` or `configureOptions` is `null`.

### `GetRequestContext(this HttpContext httpContext)`

Retrieves the `RequestContextInfo` object associated with the current request.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `RequestContextInfo?` instance, or `null` if the middleware has not been registered or the context has not been initialized.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetCorrelationId(this HttpContext httpContext)`

Gets the correlation ID for the current request.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `string` containing the correlation ID. If the middleware is not registered or the context is missing, returns an empty string.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetRequestId(this HttpContext httpContext)`

Gets the unique request ID assigned to the current request.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `string` containing the request ID. Returns an empty string if the context is unavailable.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetUserId(this HttpContext httpContext)`

Gets the user identifier extracted from the request (e.g., from a claim or header).

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `string?` containing the user ID, or `null` if no user is identified or the context is missing.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetRequestTimestamp(this HttpContext httpContext)`

Gets the UTC timestamp when the request was first processed by the middleware.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `DateTime` value. Returns `DateTime.MinValue` if the context is unavailable.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetRequestMethod(this HttpContext httpContext)`

Gets the HTTP method (e.g., GET, POST) of the current request.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `string` containing the HTTP method. Returns an empty string if the context is unavailable.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

### `GetRequestPath(this HttpContext httpContext)`

Gets the request path (e.g., `/api/orders`) of the current request.

- **Parameters**: `httpContext` – The current `HttpContext`.
- **Returns**: A `string` containing the request path. Returns an empty string if the context is unavailable.
- **Throws**: `ArgumentNullException` if `httpContext` is `null`.

## Usage

### Example 1: Register middleware and retrieve context in a controller

```csharp
// Program.cs – register middleware
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRequestContext(); // uses default options

app.MapGet("/orders/{id}", async (HttpContext context, int id) =>
{
    var correlationId = context.GetCorrelationId();
    var userId = context.GetUserId();
    // ... business logic
    return Results.Ok(new { CorrelationId = correlationId, UserId = userId });
});

app.Run();
```

### Example 2: Custom configuration and accessing full context

```csharp
// Program.cs – custom header name for correlation ID
app.UseRequestContext(options =>
{
    options.CorrelationIdHeader = "X-Custom-Correlation-Id";
    options.AutoGenerateCorrelationId = true;
});

// In a middleware or service
public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var ctxInfo = context.GetRequestContext();
        if (ctxInfo != null)
        {
            Console.WriteLine($"Request {ctxInfo.RequestId} started at {ctxInfo.Timestamp:O}");
        }
        await _next(context);
    }
}
```

## Notes

- All retrieval methods (`GetCorrelationId`, `GetRequestId`, etc.) return default values (empty string, `null`, or `DateTime.MinValue`) when the middleware has not been registered or when the `HttpContext` has not been processed by the middleware. They do **not** throw exceptions in those cases beyond the `ArgumentNullException` for a `null` `httpContext`.
- The `RequestContextInfo` object is stored in `HttpContext.Items` and is scoped to the current request. It is not shared across requests and is disposed when the request completes.
- Thread safety: The `HttpContext.Items` dictionary is not thread-safe by default. However, because the middleware writes the context once at the start of the pipeline and subsequent reads occur within the same request (single-threaded in typical ASP.NET Core execution), no synchronization is required. If you access the context from multiple threads within the same request (e.g., using `ConfigureAwait(false)` with custom synchronization), you should ensure proper locking or use a concurrent collection.
- The `GetUserId` method returns `null` when no user identity is available (e.g., anonymous requests). It does not throw if the user is not authenticated.
- The timestamp returned by `GetRequestTimestamp` is captured when the middleware runs, which is typically early in the pipeline. It may not reflect the exact moment the request reached the application if other middleware runs before it.
