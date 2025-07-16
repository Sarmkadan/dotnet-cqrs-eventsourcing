# EventsController

The `EventsController` provides read-only HTTP endpoints for querying the event store in a CQRS/event-sourcing system. It exposes operations to retrieve events, obtain event counts, export event data, list event types, and fetch timeline aggregations. All methods are asynchronous and return `IActionResult` to allow flexible HTTP responses.

## API

### `EventsController()`
Initializes a new instance of the controller.  
**Parameters:** None.  
**Returns:** Nothing (constructor).  
**Throws:** None.

### `GetEvents()`
Retrieves a list of stored events.  
**Parameters:** None.  
**Returns:** `Task<IActionResult>` – typically an `OkObjectResult` containing a collection of event records.  
**Throws:** May throw if the underlying event store is unavailable or misconfigured.

### `GetEventCount()`
Returns the total number of events in the store.  
**Parameters:** None.  
**Returns:** `Task<IActionResult>` – typically an `OkObjectResult` with an integer count.  
**Throws:** May throw if the store cannot be queried.

### `ExportEvents()`
Exports all events in a downloadable format (e.g., JSON or CSV).  
**Parameters:** None.  
**Returns:** `Task<IActionResult>` – typically a `FileResult` or `OkObjectResult` containing the exported data.  
**Throws:** May throw if serialization or file generation fails.

### `GetEventTypes()`
Returns the distinct event type names present in the store.  
**Parameters:** None.  
**Returns:** `Task<IActionResult>` – typically an `OkObjectResult` with a list of strings.  
**Throws:** May throw if the store cannot be queried.

### `GetEventTimeline()`
Returns a timeline representation of events, often grouped by date or time interval.  
**Parameters:** None.  
**Returns:** `Task<IActionResult>` – typically an `OkObjectResult` with a structured timeline (e.g., a list of time buckets containing event counts).  
**Throws:** May throw if aggregation fails.

## Usage

### Example 1: Calling endpoints via `HttpClient`

```csharp
using var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

// Get event count
var countResponse = await client.GetAsync("/api/events/count");
countResponse.EnsureSuccessStatusCode();
var count = await countResponse.Content.ReadFromJsonAsync<int>();

// Get event types
var typesResponse = await client.GetAsync("/api/events/types");
typesResponse.EnsureSuccessStatusCode();
var types = await typesResponse.Content.ReadFromJsonAsync<List<string>>();
```

### Example 2: Integration test with a test host

```csharp
[Fact]
public async Task GetEvents_ReturnsOk()
{
    using var host = new TestServer(new WebHostBuilder().UseStartup<TestStartup>());
    var client = host.CreateClient();

    var response = await client.GetAsync("/api/events");
    response.EnsureSuccessStatusCode();

    var events = await response.Content.ReadFromJsonAsync<List<EventRecord>>();
    Assert.NotNull(events);
}
```

## Notes

- **Empty store:** All endpoints return valid HTTP responses even when no events exist. `GetEvents` returns an empty collection, `GetEventCount` returns `0`, `GetEventTypes` returns an empty list, and `GetEventTimeline` returns an empty timeline.
- **Large datasets:** `GetEvents` and `ExportEvents` may return large payloads. Consider implementing pagination or streaming for production use.
- **Export format:** The default export format is implementation-specific. Clients should inspect the `Content-Type` header of the response.
- **Thread safety:** Each request creates a new controller instance. No shared mutable state is maintained across requests, so the controller is inherently thread-safe under normal ASP.NET Core request processing.
- **Error handling:** Exceptions from the event store are not caught by the controller by default. Use middleware or exception filters to return appropriate HTTP status codes (e.g., 500 Internal Server Error).
