# DiagnosticsController

The `DiagnosticsController` exposes a set of HTTP endpoints that provide operational insight into the running application. It aggregates performance counters, operation traces, cache statistics, system information, and health summaries, and also allows administrative actions such as clearing cached data or resetting metric collections.

## API

### GetPerformanceMetrics
- **Purpose:** Retrieves a snapshot of current performance metrics (e.g., request latency, throughput, CPU/memory usage).
- **Parameters:** None.
- **Return Value:** `IActionResult` containing a JSON object with the metric data on success (HTTP 200). If the metrics provider is not available, returns a 500 Internal Server Error.
- **Throws:** May propagate exceptions from the underlying metrics service, resulting in a 500 response; otherwise does not throw.

### GetOperationMetrics
- **Purpose:** Returns aggregated operation‑level metrics such as counts, success/failure ratios, and average durations for each tracked operation.
- **Parameters:** None.
- **Return Value:** `IActionResult` with a JSON payload of operation metrics (HTTP 200) or 500 if the operation tracker cannot be accessed.
- **Throws:** Same as `GetPerformanceMetrics`; any unexpected error is wrapped in a 500 response.

### GetCacheStats
- **Purpose:** Provides statistics about the application cache (hit/miss ratios, item count, memory usage).
- **Parameters:** None.
- **Return Value:** `IActionResult` holding a JSON object with cache statistics (HTTP 200) or 500 on failure.
- **Throws:** May throw if the cache abstraction throws; results in a 500 response.

### ClearCache
- **Purpose:** Asynchronously removes all entries from the application cache.
- **Parameters:** None.
- **Return Value:** `Task<IActionResult>` that completes with an empty body (HTTP 204) on success, or 500 if the clear operation fails.
- **Throws:** Any exception from the cache clear operation causes a 500 response; the method itself does not throw outside of the task.

### ClearPerformanceMetrics
- **Purpose:** Resets the collected performance metrics to their initial state.
- **Parameters:** None.
- **Return Value:** `IActionResult` with HTTP 204 on success, or 500 if the reset cannot be performed.
- **Throws:** Propagates exceptions from the metrics store as a 500 response.

### GetSystemInfo
- **Purpose:** Returns static and runtime information about the host environment (OS version, .NET version, process ID, etc.).
- **Parameters:** None.
- **Return Value:** `IActionResult` containing a JSON object with system details (HTTP 200) or 500 if information cannot be gathered.
- **Throws:** May throw if accessing system APIs fails; results in a 500 response.

### GetHealthSummary
- **Purpose:** Provides a high‑level health indicator combining subsystem statuses (e.g., database, message broker, cache).
- **Parameters:** None.
- **Return Value:** `IActionResult` with a JSON health summary (HTTP 200) or 503 Service Unavailable if any critical subsystem reports unhealthy.
- **Throws:** Does not throw directly; unhealthy states are expressed via the response status code.

## Usage

```csharp
// Example 1: Using HttpClient to fetch performance metrics from a running instance.
using var http = new HttpClient();
var response = await http.GetAsync("https://api.example.com/diagnostics/performancemetrics");
response.EnsureSuccessStatusCode();
var metricsJson = await response.Content.ReadAsStringAsync();
// Deserialize metricsJson into a DTO for further processing.
```

```csharp
// Example 2: Direct invocation in a unit test with a mocked diagnostics service.
var controller = new DiagnosticsController(mockMetricsService, mockCacheService, mockSystemInfoProvider);
var result = controller.GetOperationMetrics() as OkObjectResult;
Assert.NotNull(result);
var operationMetrics = Assert.IsType<OperationMetricsDto>(result.Value);
Assert.GreaterOrEqual(operationMetrics.TotalRequests, 0);
```

## Notes

- The controller itself is stateless; all state is held by injected services. Consequently, its methods are safe to invoke concurrently from multiple requests.
- `ClearCache` and `ClearPerformanceMetrics` modify shared state; concurrent calls may cause temporary inconsistencies (e.g., a cache read occurring while a clear is in progress). Consumers should tolerate stale data for the short duration of the operation.
- If any underlying service throws an exception, the controller translates it into an appropriate HTTP error status (generally 500, or 503 for health checks) rather than allowing the exception to bubble up the ASP.NET Core pipeline.
- The endpoints produce JSON payloads; clients should be prepared to handle missing fields gracefully, as future versions may add or remove metric properties without breaking changes.
