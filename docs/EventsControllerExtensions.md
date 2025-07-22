# EventsControllerExtensions

The `EventsControllerExtensions` class provides a set of static extension methods designed to streamline the implementation of HTTP endpoints within ASP.NET Core controllers handling event sourcing data. By encapsulating common retrieval and statistical patterns, these methods reduce boilerplate code in controller actions, offering standardized interfaces for querying events by type, time range, simple listing, and aggregate statistics while ensuring consistent `IActionResult` responses.

## API

### GetEventsByType
Retrieves a collection of events filtered specifically by their event type identifier.
*   **Purpose**: Fetches all events matching a specified type name or category from the underlying event store.
*   **Parameters**: Accepts the controller instance (`this EventsController controller`), the event type string, and optional pagination parameters (implied by standard patterns, though specific signature details depend on the underlying store implementation).
*   **Return Value**: Returns a `Task<IActionResult>` containing an `OkObjectResult` with the list of events if found, or an appropriate error result (e.g., `NotFoundResult`) if no events match the type.
*   **Throws**: Throws `ArgumentNullException` if the event type string is null or empty; may propagate store-specific exceptions if the underlying data source is unavailable.

### GetEventsByTimeRange
Retrieves events that occurred within a specific start and end timestamp.
*   **Purpose**: Queries the event store for events where the occurrence timestamp falls between the provided `startTime` and `endTime`.
*   **Parameters**: Accepts the controller instance, a `DateTime` start value, a `DateTime` end value, and optional filtering flags.
*   **Return Value**: Returns a `Task<IActionResult>` wrapping the filtered sequence of events. If the range is invalid (start > end), it typically returns a `BadRequestResult`.
*   **Throws**: Throws `ArgumentOutOfRangeException` if the time range logic is violated; throws connection-related exceptions if the event store cannot be reached.

### GetEventsSimple
Provides a basic, unfiltered retrieval mechanism for events, often used for health checks or initial data loading.
*   **Purpose**: Fetches a default subset of events (e.g., the most recent N events) without complex filtering criteria.
*   **Parameters**: Accepts the controller instance and an optional count limit.
*   **Return Value**: Returns a `Task<IActionResult>` containing the simplified event list.
*   **Throws**: Generally safe from argument exceptions unless negative limits are provided; relies on the underlying store's stability.

### GetEventStatistics
Calculates and returns aggregate metrics regarding the stored events.
*   **Purpose**: Generates summary data such as total event count, counts per type, or average events per time unit.
*   **Parameters**: Accepts the controller instance and optional scope parameters (e.g., specific aggregate IDs).
*   **Return Value**: Returns a `Task<IActionResult>` containing a statistics DTO serialized as JSON.
*   **Throws**: May throw exceptions if the calculation requires scanning a corrupted log or if the store is in an inconsistent state.

## Usage

The following examples demonstrate how to integrate these extensions into an ASP.NET Core controller to expose event sourcing data via HTTP GET endpoints.

```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventStore _eventStore;

    public EventsController(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    // Example 1: Retrieving events by a specific type
    [HttpGet("type/{eventType}")]
    public async Task<IActionResult> GetByType(string eventType)
    {
        // Delegates logic to the extension method
        return await this.GetEventsByType(eventType, pageNumber: 1, pageSize: 50);
    }

    // Example 2: Retrieving statistical summaries
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        // Delegates logic to the extension method
        return await this.GetEventStatistics(includeDetailedBreakdown: true);
    }
}
```

```csharp
[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    // Example 3: Retrieving events within a specific time window
    [HttpGet("range")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] DateTime start, 
        [FromQuery] DateTime end)
    {
        if (start > end)
        {
            return BadRequest("Start time must be before end time.");
        }

        // Delegates logic to the extension method
        return await this.GetEventsByTimeRange(start, end);
    }

    // Example 4: Simple retrieval for dashboard widgets
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestEvents()
    {
        return await this.GetEventsSimple(limit: 100);
    }
}
```

## Notes

*   **Thread Safety**: As this class consists entirely of static extension methods operating on stateless logic (delegating to the controller's injected services), the methods themselves are thread-safe. However, thread safety of the returned `IActionResult` execution depends on the underlying `IEventStore` implementation and the ASP.NET Core request pipeline.
*   **Null Handling**: Callers must ensure that string parameters (such as event types) are not null or empty before invocation, as these methods typically validate inputs immediately and throw `ArgumentNullException` to prevent invalid queries reaching the data layer.
*   **Time Zone Considerations**: When using `GetEventsByTimeRange`, ensure that the `DateTime` objects passed are normalized to UTC to avoid discrepancies caused by local server time zones versus stored event timestamps.
*   **Performance**: `GetEventStatistics` may involve full scans or heavy aggregation depending on the event store size. It is recommended to cache the results of this method if called frequently in high-throughput scenarios.
*   **Return Types**: All methods return `Task<IActionResult>`. Consumers should not assume the result is always `OkObjectResult`; proper handling of `BadRequestResult`, `NotFoundResult`, and `StatusCodeResult` is required in the calling context.
