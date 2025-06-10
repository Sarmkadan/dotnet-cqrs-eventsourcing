# ErrorHandlingMiddleware
The `ErrorHandlingMiddleware` class is designed to handle errors in a .NET application, providing a centralized mechanism for catching and processing exceptions. It allows for the registration of a global error handling mechanism, enabling the application to gracefully handle unexpected errors and provide meaningful error messages.

## API
* `public ErrorHandlingMiddleware`: The constructor for the `ErrorHandlingMiddleware` class, used to create a new instance.
* `public async Task InvokeAsync`: An asynchronous method that invokes the middleware, allowing it to process the current HTTP request and handle any errors that may occur. This method does not take any parameters and returns a `Task` that represents the asynchronous operation. It may throw an exception if an error occurs during the invocation.
* `public string ErrorId`: A property that gets the unique identifier for the error.
* `public string Message`: A property that gets the error message.
* `public string[] Details`: A property that gets additional details about the error.
* `public DateTime Timestamp`: A property that gets the timestamp when the error occurred.
* `public static IApplicationBuilder UseGlobalErrorHandling`: A static method that registers the global error handling middleware in the application's pipeline. This method takes an `IApplicationBuilder` instance as a parameter and returns the same instance, allowing for method chaining. It does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `ErrorHandlingMiddleware` class:
```csharp
// Example 1: Registering the global error handling middleware
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseGlobalErrorHandling();
app.Run();
```

```csharp
// Example 2: Creating an instance of the ErrorHandlingMiddleware class
var middleware = new ErrorHandlingMiddleware();
await middleware.InvokeAsync();
Console.WriteLine($"Error ID: {middleware.ErrorId}, Message: {middleware.Message}, Details: {string.Join(", ", middleware.Details)}");
```

## Notes
When using the `ErrorHandlingMiddleware` class, consider the following edge cases and thread-safety remarks:
* The `ErrorHandlingMiddleware` class is designed to handle errors in a thread-safe manner, allowing it to be safely used in multi-threaded environments.
* The `InvokeAsync` method may throw an exception if an error occurs during the invocation. It is recommended to handle this exception accordingly to prevent it from propagating up the call stack.
* The `UseGlobalErrorHandling` method registers the global error handling middleware in the application's pipeline. This method should be called only once during the application's startup to avoid duplicate registrations.
* The properties `ErrorId`, `Message`, `Details`, and `Timestamp` provide information about the error that occurred. These properties can be used to log the error or display it to the user.
