// existing content ...

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` provides global exception handling by converting unhandled exceptions into consistent HTTP responses. It maps domain-specific exceptions to appropriate HTTP status codes (e.g., `DomainException` → 400, unexpected errors → 500) and returns structured error details including `ErrorId`, `Message`, `Details`, and `Timestamp`.

### Usage Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // No direct registration needed for ErrorHandlingMiddleware
        // It's configured via UseGlobalErrorHandling in Configure
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseGlobalErrorHandling(); // Registers the error handling middleware
        // Other middleware registrations...
    }
}
```

Example error response structure:
```json
{
  "errorId": "a1b2c3d4",
  "message": "Domain rule violation",
  "details": ["Invalid account state"],
  "timestamp": "2024-03-20T12:34:56Z"
}
```

This middleware ensures all exceptions are logged with appropriate severity and returns predictable JSON errors to clients.
