// existing content ...

## ICacheService

The `ICacheService` interface defines a cache abstraction for storing and retrieving values with optional expiration. It provides methods for getting, setting, removing, and getting statistics about cache entries.

### Usage Example

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var cacheService = new InMemoryCacheService();

        // Set a value in cache with expiration
        await cacheService.SetAsync<string>("key", "value", TimeSpan.FromMinutes(5));

        // Get a value from cache
        var cachedValue = await cacheService.GetAsync<string>("key");
        Console.WriteLine(cachedValue); // Output: "value"

        // Get statistics about cache
        var statistics = cacheService.GetStatistics();
        Console.WriteLine($"Total Entries: {statistics.TotalEntries}");
        Console.WriteLine($"Total Hits: {statistics.TotalHits}");
        Console.WriteLine($"Expired Entries: {statistics.ExpiredEntries}");
        Console.WriteLine($"Average Entry Age: {statistics.AverageEntryAge}");

        // Remove a value from cache
        await cacheService.RemoveAsync("key");

        // Get a value from cache after removal
        cachedValue = await cacheService.GetAsync<string>("key");
        Console.WriteLine(cachedValue); // Output: null
    }
}
```

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

## RateLimitingMiddleware

The `RateLimitingMiddleware` enforces per-IP request quotas using the token bucket algorithm. It allows burst traffic while maintaining average throughput limits and returns 429 Too Many Requests with Retry-After header when the rate limit is exceeded.

### Usage Example

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseRateLimiting(options => options.TokensPerMinute = 60); // Configure rate limiting with 60 tokens per minute
        // Other middleware registrations...
    }
}
```

This middleware ensures that clients do not exceed the specified rate limit, preventing abuse and denial-of-service attacks.

## StringExtensions

The `StringExtensions` class provides a collection of utility extension methods for common string operations including slugification, case conversion, padding, and formatting. These methods are used throughout the framework for URL generation, logging, and data transformation to ensure consistent formatting and validation.

### Usage Example

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Convert to URL-friendly slug
        var title = "Hello World Event Sourcing";
        var slug = title.ToSlug();
        Console.WriteLine(slug); // Output: "hello-world-event-sourcing"

        // Convert between case formats
        var camelCase = "AccountCreatedEvent".ToCamelCase();
        Console.WriteLine(camelCase); // Output: "accountCreatedEvent"

        var pascalCase = "user_account_created".ToPascalCase();
        Console.WriteLine(pascalCase); // Output: "UserAccountCreated"

        var snakeCase = "OrderProcessingComplete".ToSnakeCase();
        Console.WriteLine(snakeCase); // Output: "order_processing_complete"

        // Truncate and pad strings
        var longText = "This is a very long text that needs to be shortened";
        var truncated = longText.Truncate(20);
        Console.WriteLine(truncated); // Output: "This is a very long..."

        var padded = "42".PadLeft(5, '0');
        Console.WriteLine(padded); // Output: "00042"

        // Check formats and validate
        var isEmailValid = "test@example.com".IsValidEmail();
        Console.WriteLine(isEmailValid); // Output: True

        var isGuidValid = "550e8400-e29b-41d4-a716-446655440000".IsValidGuid();
        Console.WriteLine(isGuidValid); // Output: True

        var isNumeric = "12345".IsNumeric();
        Console.WriteLine(isNumeric); // Output: True

        // Remove whitespace and ensure prefixes/suffixes
        var noWhitespace = "Hello   World".RemoveWhitespace();
        Console.WriteLine(noWhitespace); // Output: "HelloWorld"

        var withPrefix = "world".EnsureStartsWith("hello_");
        Console.WriteLine(withPrefix); // Output: "hello_world"

        var withSuffix = "hello".EnsureEndsWith("_world");
        Console.WriteLine(withSuffix); // Output: "hello_world"

        // Repeat strings
        var repeated = "*".Repeat(5);
        Console.WriteLine(repeated); // Output: "*****"
    }
}
```

