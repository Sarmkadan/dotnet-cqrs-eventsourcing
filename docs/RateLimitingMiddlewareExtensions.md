# RateLimitingMiddlewareExtensions

The `RateLimitingMiddlewareExtensions` class provides a set of static extension methods and utility functions designed to manage the configuration and runtime state of rate limiting within the CQRS and Event Sourcing middleware pipeline. It serves as the primary interface for enabling, disabling, and querying the status of request throttling mechanisms without requiring direct manipulation of the underlying middleware instances.

## API

### `ConfigureRateLimiting`
Configures the rate limiting parameters for the application pipeline. This method initializes the throttling rules, such as request counts and time windows, based on the provided configuration settings.
*   **Parameters**: Accepts the necessary configuration arguments (typically an `IServiceCollection` or specific options object) required to define the rate limit policies.
*   **Return Value**: `void`
*   **Exceptions**: Throws an `InvalidOperationException` if called after the middleware pipeline has already been built and executed, or if the provided configuration is null or invalid.

### `IsRateLimitingActive`
Determines whether the rate limiting middleware is currently enabled and enforcing policies on incoming requests.
*   **Parameters**: None.
*   **Return Value**: `bool` – Returns `true` if rate limiting is active; otherwise, `false`.
*   **Exceptions**: Does not throw exceptions under normal operation.

### `GetRateLimitStatus`
Retrieves a detailed string representation of the current rate limiting state, including active policy names, current usage metrics, and configuration summaries.
*   **Parameters**: None.
*   **Return Value**: `string` – A formatted status message describing the current operational state of the rate limiter.
*   **Exceptions**: May throw an `InvalidOperationException` if the internal status tracker has not been initialized.

### `DisableRateLimiting`
Immediately disables the rate limiting logic, allowing all subsequent requests to bypass throttling checks until explicitly re-enabled or the application restarts.
*   **Parameters**: None.
*   **Return Value**: `void`
*   **Exceptions**: Does not throw exceptions; calling this method when rate limiting is already disabled is a no-op.

## Usage

### Example 1: Configuring Rate Limiting during Startup
The following example demonstrates how to configure rate limiting policies within the service collection setup phase of the application.

```csharp
using DotNetCqrsEventSourcing.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Other service registrations...

        // Configure rate limiting with specific policies
        services.ConfigureRateLimiting(options =>
        {
            options.GlobalLimit = 100;
            options.WindowInSeconds = 60;
            options.PerClientLimit = 20;
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        if (RateLimitingMiddlewareExtensions.IsRateLimitingActive())
        {
            Console.WriteLine("Rate limiting is enforced.");
        }
        
        // Add middleware to pipeline...
    }
}
```

### Example 2: Runtime Status Check and Emergency Disable
This example illustrates checking the current status of the rate limiter and dynamically disabling it in response to a critical system event or maintenance window.

```csharp
using DotNetCqrsEventSourcing.Extensions;

public class MaintenanceService
{
    public void EnterMaintenanceMode()
    {
        // Log current status before changing state
        string status = RateLimitingMiddlewareExtensions.GetRateLimitStatus();
        Console.WriteLine($"Current State: {status}");

        if (RateLimitingMiddlewareExtensions.IsRateLimitingActive())
        {
            Console.WriteLine("Disabling rate limiting for maintenance window...");
            RateLimitingMiddlewareExtensions.DisableRateLimiting();
            
            // Verify disablement
            if (!RateLimitingMiddlewareExtensions.IsRateLimitingActive())
            {
                Console.WriteLine("Rate limiting successfully disabled.");
            }
        }
    }
}
```

## Notes

*   **Thread Safety**: The state management methods (`IsRateLimitingActive`, `DisableRateLimiting`, `GetRateLimitStatus`) are designed to be thread-safe, utilizing atomic operations or internal locking to ensure consistent state reads and writes across concurrent requests. However, `ConfigureRateLimiting` must only be invoked during the application startup phase before the request pipeline is locked; calling it concurrently with request processing may result in undefined behavior or exceptions.
*   **State Persistence**: The disabled state set by `DisableRateLimiting` is held in memory for the lifetime of the application process. Restarting the application will revert the system to the configuration defined in `ConfigureRateLimiting`.
*   **Initialization Dependency**: Invoking `GetRateLimitStatus` or `IsRateLimitingActive` prior to calling `ConfigureRateLimiting` may result in an `InvalidOperationException` or a default "inactive" state, depending on the specific implementation of the internal initializer. Ensure configuration occurs early in the host building process.
