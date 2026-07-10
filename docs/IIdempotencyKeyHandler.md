# IIdempotencyKeyHandler

The `IIdempotencyKeyHandler` interface and its implementations manage idempotency keys in HTTP request processing to ensure that repeated requests with the same key return the same response, preventing duplicate side effects. This is particularly useful in scenarios where clients may retry requests due to network issues or timeouts, ensuring consistency in distributed systems.

## API

### `InMemoryIdempotencyKeyHandler`
A concrete implementation of `IIdempotencyKeyHandler` that stores idempotency results in memory. Suitable for development or single-instance deployments where persistence across restarts is not required.

### `Task<IdempotencyResult?> GetPreviousResultAsync(string key)`
Retrieves a previously recorded result for the given idempotency key.

- **Parameters**:
  - `key` (`string`): The idempotency key to look up.
- **Returns**:
  - `Task<IdempotencyResult?>`: The recorded result if the key exists; otherwise, `null`.
- **Throws**: None.

### `Task RecordResultAsync(string key, IdempotencyResult result)`
Records the result of an idempotent operation for the given key.

- **Parameters**:
  - `key` (`string`): The idempotency key to associate with the result.
  - `result` (`IdempotencyResult`): The result to store.
- **Returns**:
  - `Task`: A task representing the asynchronous operation.
- **Throws**: None.

### `Task ClearAsync()`
Clears all recorded idempotency results. Useful for testing or resetting state.

- **Returns**:
  - `Task`: A task representing the asynchronous operation.
- **Throws**: None.

### `IdempotencyResult Result`
The result associated with an idempotency key. This property is part of the `IdempotencyResult` class and holds the response data.

### `DateTime RecordedAt`
The timestamp when the idempotency result was recorded. This property is part of the `IdempotencyResult` class.

### `int StatusCode`
The HTTP status code of the recorded response. This property is part of the `IdempotencyResult` class.

### `string ResponseBody`
The body of the recorded response. This property is part of the `IdempotencyResult` class.

### `IdempotencyMiddleware`
Middleware that integrates idempotency key handling into the ASP.NET Core request pipeline. It intercepts requests, checks for idempotency keys, and either returns cached responses or processes new requests.

### `Task InvokeAsync(HttpContext context)`
Processes an HTTP request, checking for an idempotency key and handling the request accordingly.

- **Parameters**:
  - `context` (`HttpContext`): The HTTP context for the current request.
- **Returns**:
  - `Task`: A task representing the asynchronous operation.
- **Throws**: None.

### `static IApplicationBuilder UseIdempotency(IApplicationBuilder app)`
Extension method to add `IdempotencyMiddleware` to the application's request pipeline.

- **Parameters**:
  - `app` (`IApplicationBuilder`): The application builder.
- **Returns**:
  - `IApplicationBuilder`: The updated application builder.
- **Throws**: None.

### `static IServiceCollection AddIdempotency(IServiceCollection services, Action<IdempotencyOptions> configureOptions)`
Extension method to register `IIdempotencyKeyHandler` and related services in the dependency injection container.

- **Parameters**:
  - `services` (`IServiceCollection`): The service collection.
  - `configureOptions` (`Action<IdempotencyOptions>`): A delegate to configure idempotency options.
- **Returns**:
  - `IServiceCollection`: The updated service collection.
- **Throws**: None.

## Usage

### Example 1: Basic Setup in ASP.NET Core
