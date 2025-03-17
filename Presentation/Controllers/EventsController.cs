// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Formatters;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// HTTP API for event stream management and inspection.
/// Provides endpoints for viewing event history, debugging, and administrative operations.
/// Events are immutable once persisted - this API is read-only except for admin operations.
/// Used for auditing, debugging event sourcing issues, and data analysis.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : BaseApiController
{
    private readonly IEventStore _eventStore;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ICsvFormatter _csvFormatter;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventStore eventStore,
        IJsonFormatter jsonFormatter,
        ICsvFormatter csvFormatter,
        ILogger<EventsController> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _jsonFormatter = jsonFormatter ?? throw new ArgumentNullException(nameof(jsonFormatter));
        _csvFormatter = csvFormatter ?? throw new ArgumentNullException(nameof(csvFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /events/{aggregateId} - Retrieves complete event history for an aggregate.
    /// Returns events in causality order (oldest first).
    /// Useful for auditing, debugging, and verification.
    /// </summary>
    [HttpGet("{aggregateId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvents(
        [FromRoute] string aggregateId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        _logger.LogInformation(
            "Fetching events for aggregate {AggregateId} (skip: {Skip}, take: {Take})",
            aggregateId,
            skip,
            take
        );

        try
        {
            var allEvents = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);

            if (allEvents.Count == 0)
            {
                return NotFound(new { success = false, message = $"No events found for aggregate {aggregateId}" });
            }

            var pagedEvents = allEvents.Skip(skip).Take(take).ToList();

            return Ok(new
            {
                success = true,
                aggregateId = aggregateId,
                totalCount = allEvents.Count,
                pageSize = pagedEvents.Count,
                skip = skip,
                events = pagedEvents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events for aggregate {AggregateId}", aggregateId);
            return StatusCode(500, new { success = false, message = "Error retrieving events" });
        }
    }

    /// <summary>
    /// GET /events/{aggregateId}/count - Returns the total number of events for an aggregate.
    /// Useful for monitoring event stream growth.
    /// </summary>
    [HttpGet("{aggregateId}/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventCount(
        [FromRoute] string aggregateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            return Ok(new
            {
                success = true,
                aggregateId = aggregateId,
                eventCount = events.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event count for aggregate {AggregateId}", aggregateId);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /events/{aggregateId}/export?format=json|csv - Exports events in specified format.
    /// Supports JSON and CSV export for data analysis and reporting.
    /// </summary>
    [HttpGet("{aggregateId}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportEvents(
        [FromRoute] string aggregateId,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        _logger.LogInformation("Exporting events for aggregate {AggregateId} in {Format} format", aggregateId, format);

        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);

            if (events.Count == 0)
            {
                return NotFound(new { success = false, message = "No events found" });
            }

            return format.ToLower() switch
            {
                "csv" => ExportAsCSV(aggregateId, events),
                "json" => ExportAsJSON(aggregateId, events),
                _ => BadRequest(new { success = false, message = "Unsupported format. Use 'json' or 'csv'." })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting events for aggregate {AggregateId}", aggregateId);
            return StatusCode(500, new { success = false, message = "Error exporting events" });
        }
    }

    /// <summary>
    /// GET /events/{aggregateId}/types - Returns unique event types emitted by this aggregate.
    /// Useful for understanding the aggregate's behavior and event patterns.
    /// </summary>
    [HttpGet("{aggregateId}/types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTypes(
        [FromRoute] string aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            var eventTypes = events
                .Select(e => e.GetType().Name)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return Ok(new
            {
                success = true,
                aggregateId = aggregateId,
                eventTypes = eventTypes,
                uniqueTypeCount = eventTypes.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event types for aggregate {AggregateId}", aggregateId);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /events/{aggregateId}/timeline - Returns event timeline with human-readable timestamps.
    /// Useful for understanding the chronological sequence of events.
    /// </summary>
    [HttpGet("{aggregateId}/timeline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTimeline(
        [FromRoute] string aggregateId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);

            var timeline = events.Select((e, index) => new
            {
                sequence = index + 1,
                eventType = e.GetType().Name,
                timestamp = e.Timestamp,
                correlationId = e.CorrelationId,
                aggregateId = e.AggregateId
            }).ToList();

            return Ok(new
            {
                success = true,
                aggregateId = aggregateId,
                eventCount = timeline.Count,
                timeSpan = events.Count > 0 ? TimeSpan.FromTicks(events[^1].Timestamp.Ticks - events[0].Timestamp.Ticks) : TimeSpan.Zero,
                timeline = timeline
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event timeline for aggregate {AggregateId}", aggregateId);
            return StatusCode(500, new { success = false });
        }
    }

    private IActionResult ExportAsJSON(string aggregateId, List<dynamic> events)
    {
        var json = _jsonFormatter.Format(new { aggregateId, events }, new() { PrettyPrint = true });
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"{aggregateId}-events.json");
    }

    private IActionResult ExportAsCSV(string aggregateId, List<dynamic> events)
    {
        var csv = _csvFormatter.Format(events);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{aggregateId}-events.csv");
    }
}
