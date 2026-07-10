# RateLimitingMiddleware

The `RateLimitingMiddleware` component provides request throttling capabilities for ASP.NET Core applications within the `dotnet-cqrs-eventsourcing` project. It implements a token bucket algorithm to control the rate of incoming HTTP requests, ensuring that services are not overwhelmed by excessive traffic. The middleware tracks access times and token availability to determine whether a specific request should be processed or rejected based on configured limits.

## API

### Constructors

#### `public RateLimitingMiddleware()`
Initializes a new instance of the `RateLimitingMiddleware` class. This constructor sets up the internal state required for tracking request rates, typically initializing with default or disabled configurations until explicitly configured via properties or static options.

### Methods

#### `public async Task InvokeAsync(HttpContext context)`
The primary entry point executed by the ASP.NET Core pipeline for each incoming request.
*   **Purpose**: Evaluates whether the current request adheres to the rate limiting rules. If allowed, it proceeds to the next middleware in the pipeline; if denied, it returns an appropriate HTTP status code (typically 429 Too Many Requests).
*   **Parameters**:
    *   `context`: The `HttpContext` for the current request, used to access request details and set response status.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: May throw exceptions if the underlying `TokenBucket` logic encounters an invalid state or if the next middleware in the pipeline throws.

#### `public static IApplicationBuilder UseRateLimiting(IApplicationBuilder app)`
An extension method used to register the middleware within the application request pipeline.
*   **Purpose**: Adds the `RateLimitingMiddleware` to the `IApplicationBuilder` pipeline.
*   **Parameters**:
    *   `app`: The `IApplicationBuilder` instance to configure.
*   **Return Value**: The same `IApplicationBuilder` instance to allow for fluent chaining.
*   **Exceptions**: Throws `ArgumentNullException` if `app` is null.

### Properties

#### `public DateTime LastAccessTime`
Gets or sets the timestamp of the most recent request processed by this middleware instance.
*   **Purpose**: Used internally to calculate the time elapsed since the last token refill or request evaluation.
*   **Value**: A `DateTime` object representing the last access moment.

#### `public TokenBucket TokenBucket`
Gets or sets the `TokenBucket` instance responsible for the core rate-limiting logic.
*   **Purpose**: Encapsulates the state of available tokens and the refill rate. The middleware delegates the decision to allow or deny requests to this object.
*   **Value**: An instance of `TokenBucket`.

#### `public bool AllowRequest`
Gets a value indicating whether the current request is permitted under the active rate limiting rules.
*   **Purpose**: Provides the immediate result of the rate limit check. This property is typically evaluated within `InvokeAsync`.
*   **Value**: `true` if the request is allowed; `false` if the limit has been exceeded.

#### `public double TokensPerMinute`
Gets or sets the rate at which tokens are replenished in the bucket.
*   **Purpose**: Defines the throughput capacity of the system, specifying how many requests are permitted per minute on average.
*   **Value**: A `double` representing the number of tokens added per minute.

#### `public bool Enabled`
Gets or sets a value indicating whether the rate limiting logic is active.
*   **Purpose**: Allows for dynamic enabling or disabling of the middleware without removing it from the pipeline. If `false`, all requests are typically allowed to pass through.
*   **Value**: `true` if rate limiting is enforced; `false` otherwise.

### Static Members

#### `public static RateLimitOptions Default`
Provides a predefined set of options representing a standard rate limiting configuration.
*   **Purpose**: Offers a sensible default configuration for production environments (e.g., a specific non-zero `TokensPerMinute` value).
*   **Value**: A `RateLimitOptions` instance.

#### `public static RateLimitOptions Disabled`
Provides a predefined set of options that effectively disables rate limiting.
*   **Purpose**: Useful for development environments or scenarios where throttling must be bypassed globally.
*   **Value**: A `RateLimitOptions` instance configured to allow unlimited requests.

## Usage

### Example 1: Registering Middleware with Default Options
The following example demonstrates how to add the rate limiting middleware to the application pipeline during startup, utilizing the default configuration.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Other middleware (e.g., error handling, routing)
    
    // Enable rate limiting with default settings
    app.UseRateLimiting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### Example 2: Custom Configuration and Dynamic Control
This example illustrates configuring specific throughput limits and dynamically checking the enabled status before processing sensitive operations.

```csharp
// Configure specific limits
var middleware = new RateLimitingMiddleware
{
    TokensPerMinute = 60.0, // Allow 60 requests per minute
    Enabled = true
};

// In a custom middleware or startup logic
if (middleware.Enabled)
{
    // Simulate invocation context
    await middleware.InvokeAsync(httpContext);
    
    if (!middleware.AllowRequest)
    {
        // Handle rejection logic explicitly if needed
        httpContext.Response.StatusCode = 429;
        await httpContext.Response.WriteAsync("Rate limit exceeded.");
        return;
    }
}
```

## Notes

*   **Thread Safety**: The `LastAccessTime` and `TokenBucket` properties are mutable. In a multi-threaded ASP.NET Core environment where a single middleware instance may handle concurrent requests, access to these properties must be synchronized by the internal `TokenBucket` implementation. If `TokenBucket` is not internally thread-safe, external locking mechanisms are required when modifying `TokensPerMinute` or `LastAccessTime` concurrently with `InvokeAsync`.
*   **State Persistence**: The `RateLimitingMiddleware` typically maintains state in memory. Restarting the application will reset the `LastAccessTime` and refill the `TokenBucket`, potentially allowing a burst of traffic immediately after deployment.
*   **Disabled State**: When `Enabled` is set to `false`, the `InvokeAsync` method should bypass the `TokenBucket` check entirely. However, `LastAccessTime` might still be updated depending on the specific implementation logic, which could affect calculations if the middleware is re-enabled later.
*   **Granularity**: The `TokensPerMinute` property accepts a `double`, allowing for fractional rates (e.g., 0.5 tokens per minute for very strict limiting). Ensure the `TokenBucket` implementation supports fractional token accumulation to avoid precision loss.
