#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Presentation.Controllers;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// Extension methods for <see cref="EventsController"/> that provide additional functionality
/// for event stream management, filtering, and analysis without modifying the original controller.
/// </summary>
public static class EventsControllerExtensions
{
    /// <summary>
    /// Filters events by type and returns paginated results.
    /// Useful for debugging specific event types or analyzing event patterns.
    /// </summary>
    /// <param name="controller">The events controller instance</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="eventTypeName">The name of the event type to filter by (e.g., "OrderCreated", "PaymentProcessed")</param>
    /// <param name="skip">Number of events to skip</param>
    /// <param name="take">Maximum number of events to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered and paginated event results</returns>
    public static async Task<IActionResult> GetEventsByType(
        this EventsController controller,
        string aggregateId,
        string eventTypeName,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);

        controller.Log?.LogInformation(
            "Fetching events of type {EventTypeName} for aggregate {AggregateId} (skip: {Skip}, take: {Take})",
            eventTypeName,
            aggregateId,
            skip,
            take
        );

        try
        {
            var allEvents = await controller.GetEvents(aggregateId, 0, int.MaxValue, cancellationToken);

            if (allEvents is not OkObjectResult okResult || okResult.Value is not dynamic result)
            {
                return new ObjectResult(new { success = false, message = "Failed to retrieve events" })
                {
                    StatusCode = 500
                };
            }

            var events = ((IEnumerable<dynamic>)result.events).ToList();
            var filteredEvents = events
                .Where(e => e.GetType().Name.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase))
                .Skip(skip)
                .Take(take)
                .ToList();

            return new OkObjectResult(new
            {
                success = true,
                aggregateId = aggregateId,
                eventType = eventTypeName,
                totalCount = events.Count(e => e.GetType().Name.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase)),
                pageSize = filteredEvents.Count,
                skip = skip,
                events = filteredEvents
            });
        }
        catch (Exception ex)
        {
            controller.Log?.LogError(ex, "Error filtering events by type {EventTypeName} for aggregate {AggregateId}", eventTypeName, aggregateId);
            return new ObjectResult(new { success = false, message = "Error filtering events by type" })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Gets events within a specific time range.
    /// Useful for analyzing event patterns during specific time periods or debugging temporal issues.
    /// </summary>
    /// <param name="controller">The events controller instance</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="startTime">Start of time range (inclusive)</param>
    /// <param name="endTime">End of time range (inclusive)</param>
    /// <param name="skip">Number of events to skip</param>
    /// <param name="take">Maximum number of events to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Events within the specified time range</returns>
    public static async Task<IActionResult> GetEventsByTimeRange(
        this EventsController controller,
        string aggregateId,
        DateTime startTime,
        DateTime endTime,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        controller.Log?.LogInformation(
            "Fetching events for aggregate {AggregateId} in time range {StartTime} to {EndTime} (skip: {Skip}, take: {Take})",
            aggregateId,
            startTime.ToString("o", CultureInfo.InvariantCulture),
            endTime.ToString("o", CultureInfo.InvariantCulture),
            skip,
            take
        );

        try
        {
            var allEvents = await controller.GetEvents(aggregateId, 0, int.MaxValue, cancellationToken);

            if (allEvents is not OkObjectResult okResult || okResult.Value is not dynamic result)
            {
                return new ObjectResult(new { success = false, message = "Failed to retrieve events" })
                {
                    StatusCode = 500
                };
            }

            var events = ((IEnumerable<dynamic>)result.events).ToList();
            var filteredEvents = events
                .Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime)
                .OrderBy(e => e.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToList();

            return new OkObjectResult(new
            {
                success = true,
                aggregateId = aggregateId,
                timeRangeStart = startTime.ToString("o"),
                timeRangeEnd = endTime.ToString("o"),
                totalCount = events.Count(e => e.Timestamp >= startTime && e.Timestamp <= endTime),
                pageSize = filteredEvents.Count,
                skip = skip,
                events = filteredEvents
            });
        }
        catch (Exception ex)
        {
            controller.Log?.LogError(ex, "Error filtering events by time range for aggregate {AggregateId}", aggregateId);
            return new ObjectResult(new { success = false, message = "Error filtering events by time range" })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Gets paginated events with simplified response format for client applications.
    /// Reduces payload size by excluding metadata and providing only essential event data.
    /// </summary>
    /// <param name="controller">The events controller instance</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="skip">Number of events to skip</param>
    /// <param name="take">Maximum number of events to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simplified paginated event results</returns>
    public static async Task<IActionResult> GetEventsSimple(
        this EventsController controller,
        string aggregateId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await controller.GetEvents(aggregateId, skip, take, cancellationToken);

        if (result is OkObjectResult okResult && okResult.Value is dynamic originalValue)
        {
            return new OkObjectResult(new
            {
                success = originalValue.success,
                aggregateId = originalValue.aggregateId,
                events = ((IEnumerable<dynamic>)originalValue.events).Select(e => new
                {
                    type = e.GetType().Name,
                    data = e.Data,
                    timestamp = e.Timestamp,
                    correlationId = e.CorrelationId
                }),
                totalCount = originalValue.totalCount,
                pageSize = originalValue.pageSize,
                skip = originalValue.skip
            });
        }

        return result;
    }

    /// <summary>
    /// Gets event statistics including growth rate and average time between events.
    /// Useful for monitoring aggregate health and identifying unusual patterns.
    /// </summary>
    /// <param name="controller">The events controller instance</param>
    /// <param name="aggregateId">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event statistics including growth metrics</returns>
    public static async Task<IActionResult> GetEventStatistics(
        this EventsController controller,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        controller.Log?.LogInformation("Fetching event statistics for aggregate {AggregateId}", aggregateId);

        try
        {
            var countResult = await controller.GetEventCount(aggregateId, cancellationToken);

            if (countResult is not OkObjectResult countOkResult || countOkResult.Value is not dynamic countValue)
            {
                return new ObjectResult(new { success = false, message = "Failed to retrieve event count" })
                {
                    StatusCode = 500
                };
            }

            var eventCount = (int)countValue.eventCount;

            if (eventCount == 0)
            {
                return new OkObjectResult(new
                {
                    success = true,
                    aggregateId = aggregateId,
                    eventCount = 0,
                    growthRate = 0.0,
                    averageTimeBetweenEvents = TimeSpan.Zero,
                    firstEventTime = (DateTime?)null,
                    lastEventTime = (DateTime?)null
                });
            }

            var eventsResult = await controller.GetEvents(aggregateId, 0, int.MaxValue, cancellationToken);

            if (eventsResult is not OkObjectResult eventsOkResult || eventsOkResult.Value is not dynamic eventsValue)
            {
                return new ObjectResult(new { success = false, message = "Failed to retrieve events for statistics" })
                {
                    StatusCode = 500
                };
            }

            var events = ((IEnumerable<dynamic>)eventsValue.events).ToList();
            var firstEventTime = events.Min(e => (DateTime)e.Timestamp);
            var lastEventTime = events.Max(e => (DateTime)e.Timestamp);
            var timeSpan = lastEventTime - firstEventTime;
            var averageTimeBetweenEvents = timeSpan.TotalMilliseconds > 0 && eventCount > 1
                ? TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds / (eventCount - 1))
                : TimeSpan.Zero;

            return new OkObjectResult(new
            {
                success = true,
                aggregateId = aggregateId,
                eventCount = eventCount,
                growthRate = eventCount > 0 && timeSpan.TotalDays > 0
                    ? Math.Round(eventCount / timeSpan.TotalDays, 2)
                    : 0.0,
                averageTimeBetweenEvents = averageTimeBetweenEvents,
                firstEventTime = firstEventTime,
                lastEventTime = lastEventTime,
                timeRangeDays = Math.Round(timeSpan.TotalDays, 2)
            });
        }
        catch (Exception ex)
        {
            controller.Log?.LogError(ex, "Error retrieving event statistics for aggregate {AggregateId}", aggregateId);
            return new ObjectResult(new { success = false, message = "Error retrieving event statistics" })
            {
                StatusCode = 500
            };
        }
    }
}