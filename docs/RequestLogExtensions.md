# RequestLogExtensions

The `RequestLogExtensions` class provides static extension methods for `HttpRequest` that simplify common logging and correlation tasks in CQRS/event‑sourcing applications. These helpers let you classify requests as read‑only or write operations, retrieve client IP address and user‑agent information, and guarantee a correlation identifier is available for tracing.

## API

### IsReadOnlyOperation
```csharp
public static bool IsReadOnlyOperation(this HttpRequest request)
```
- **Purpose**: Determines whether the HTTP method of the request is considered read‑only (GET, HEAD, OPTIONS, TRACE).  
- **Parameters**:  
  - `request`: The `HttpRequest` to evaluate. Must not be null.  
- **Return value**: `true` if the method is read‑only; otherwise `false`.  
- **Exceptions**: Throws `ArgumentNullException` when `request` is null.

### IsWriteOperation
```csharp
public static bool IsWriteOperation(this HttpRequest request)
```
- **Purpose**: Determines whether the HTTP method of the request is considered a write operation (POST, PUT, PATCH, DELETE).  
- **Parameters**:  
  - `request`: The `HttpRequest` to evaluate. Must not be null.  
- **Return value**: `true` if the method is a write operation; otherwise `false`.  
- **Exceptions**: Throws `ArgumentNullException` when `request` is null.

### GetClientIpAddress
```csharp
public static string GetClientIpAddress(this HttpRequest request)
```
- **Purpose**: Attempts to obtain the original client IP address by examining common client IP, checking headers such as `X-Forwarded-For` and `X-Real-IP` before falling back to the connection’s remote IP address.  
- **Parameters**:  
  - `request`: The `HttpRequest` to inspect. Must not be null.  
- **Return value**: A string containing the client IP address, or an empty string if none could be determined.  
- **Exceptions**: Throws `ArgumentNullException` when `request` is null. Malformed header values are ignored and do not cause exceptions.

### GetUserAgent
```csharp
public static string? GetUserAgent(this HttpRequest request)
```
- **Purpose**: Retrieves the value of the `User-Agent` header if present.  
- **Parameters**:  
  - `request`: The `HttpRequest` to inspect. Must not be null.  
- **Return value**: The User‑Agent string, or `null` when the header is absent.  
- **Exceptions**: Throws `ArgumentNullException` when `request` is null.

### EnsureCorrelationId
```csharp
public static string EnsureCorrelationId(this HttpRequest request)
```
- **Purpose**: Returns an existing correlation identifier from the request (`X-Correlation-Id` or `Traceparent` header) or creates a new GUID‑based identifier and stores it in `HttpContext.Items` for downstream components.  
- **Parameters**:  
  - `request`: The `HttpRequest` to work with. Must not be null.  
- **Return value**: A non‑null correlation identifier string.  
- **Exceptions**:  
  - Throws `ArgumentNullException` when `request` is null.  
  - Throws `InvalidOperationException` if the underlying `HttpContext` cannot be accessed (e.g., when called outside of an ASP.NET Core pipeline).

## Usage

```csharp
// Example inside ASP.NET Core middleware
if (context.Request.IsReadOnlyOperation())
{
    _logger.LogInformation("Read‑only request from {Ip}", context.Request.GetClientIpAddress());
}
else if (context.Request.IsWriteOperation())
{
    _logger.LogInformation("Write request; User‑Agent: {Ua}", context.Request.GetUserAgent() ?? "<unknown>");
}

// Ensure a correlation ID is present for tracing
string correlationId = context.Request.EnsureCorrelationId();
_context.Items["CorrelationId"] = correlationId; // propagate to other services
```

```csharp
// Example logging service method
public void LogRequest(HttpRequest request)
{
    var ip = request.GetClientIpAddress();
    var agent = request.GetUserAgent();
    var isWrite = request.IsWriteOperation();

    _log.Write($"IP={ip}, Agent={agent ?? "none"}, Operation={(isWrite ? "write" : "read")}");
}
```

## Notes

- All extension methods are stateless and operate only on the supplied `HttpRequest` instance, making them thread‑safe for concurrent use on different request objects.  
- Supplying a `null` `HttpRequest` to any member results in an `ArgumentNullException`; callers should validate the request when it originates from non‑ASP.NET Core sources.  
- `GetClientIpAddress` returns an empty string when no address can be resolved; treat this as “unknown” rather than assuming a valid IP.  
- `GetUserAgent` may return `null`; consuming code must handle the null case explicitly.  
- `EnsureCorrelationId` generates a new identifier only when none is present in the request headers. The generated ID is stored in `HttpContext.Items` to guarantee consistency across the pipeline. If the method is invoked outside of an ASP.NET Core context where `HttpContext` is unavailable, it throws an `InvalidOperationException`.  
- Header look‑ups are case‑insensitive as per ASP.NET Core conventions; custom headers with alternative casing are still matched.
