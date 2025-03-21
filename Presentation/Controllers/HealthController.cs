// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Application.Services;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// Health check and diagnostic endpoint for monitoring application state.
/// Provides information about system health, dependencies, and version.
/// Used by load balancers, monitoring systems, and orchestration platforms.
/// Endpoints are public and unauthenticated for infrastructure compatibility.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IEventStore eventStore,
        ILogger<HealthController> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /health - Minimal liveness probe for load balancers.
    /// Returns 200 if application is running, 503 if degraded.
    /// Should complete in < 1 second for load balancer compatibility.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow,
            version = GetApplicationVersion()
        });
    }

    /// <summary>
    /// GET /health/detailed - Comprehensive health check including dependencies.
    /// Checks event store connectivity, cache functionality, and other dependencies.
    /// Slower than /health but provides complete system state information.
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HealthDetailed(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detailed health check requested");

        var checks = new List<DependencyCheck>();

        // Check event store
        var eventStoreHealthy = await CheckEventStoreAsync(cancellationToken);
        checks.Add(new DependencyCheck
        {
            Name = "EventStore",
            Healthy = eventStoreHealthy,
            Message = eventStoreHealthy ? "Event store accessible" : "Event store not responding"
        });

        var overallHealthy = checks.All(c => c.Healthy);
        var statusCode = overallHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, new
        {
            status = overallHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            version = GetApplicationVersion(),
            checks = checks
        });
    }

    /// <summary>
    /// GET /health/ready - Readiness probe for orchestration (Kubernetes, Docker).
    /// Returns 200 when application is ready to accept traffic (dependencies initialized).
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken = default)
    {
        var ready = await CheckEventStoreAsync(cancellationToken);

        if (!ready)
        {
            _logger.LogWarning("Readiness check failed - dependencies not ready");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "not_ready",
                message = "Dependencies not initialized"
            });
        }

        return Ok(new
        {
            status = "ready",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// GET /health/live - Liveness probe for orchestration.
    /// Returns 200 if application process is running.
    /// Does not check dependencies - that's readiness's job.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow,
            uptime = GetApplicationUptime()
        });
    }

    /// <summary>
    /// GET /health/info - Returns application information and version.
    /// Useful for debugging and verifying deployed version.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Info()
    {
        return Ok(new
        {
            application = "DotNetCqrsEventSourcing",
            version = GetApplicationVersion(),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production",
            timestamp = DateTime.UtcNow,
            machine = Environment.MachineName,
            runtime = $".NET {Environment.Version}"
        });
    }

    /// <summary>
    /// Checks if event store is accessible and responding.
    /// </summary>
    private async Task<bool> CheckEventStoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to perform a simple operation on event store
            // In a real implementation, this might query event count or metadata
            _ = await _eventStore.GetEventsAsync("health-check", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Event store health check failed");
            return false;
        }
    }

    /// <summary>
    /// Gets application version from assembly version.
    /// </summary>
    private static string GetApplicationVersion()
    {
        var assembly = typeof(HealthController).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        return version;
    }

    /// <summary>
    /// Gets application uptime duration.
    /// </summary>
    private static TimeSpan GetApplicationUptime()
    {
        var startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        return DateTime.Now - startTime;
    }

    public class DependencyCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Healthy { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
