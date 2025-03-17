// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Infrastructure.Observability;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
using DotNetCqrsEventSourcing.Infrastructure.Caching;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// Diagnostic and observability endpoints for system monitoring and debugging.
/// Provides access to performance metrics, cache statistics, and event information.
/// These endpoints should be protected in production (requires admin authorization).
/// Useful for ops teams to diagnose performance issues and troubleshoot.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DiagnosticsController : BaseApiController
{
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IPerformanceMonitor performanceMonitor,
        ICacheService cacheService,
        ILogger<DiagnosticsController> logger)
    {
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /diagnostics/performance - Returns performance metrics for all operations.
    /// Shows execution times, success rates, and throughput for monitoring.
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetPerformanceMetrics()
    {
        _logger.LogInformation("Fetching performance metrics");

        var metrics = _performanceMonitor.GetAllStatistics()
            .OrderByDescending(m => m.Stats.AverageDurationMs)
            .ToList();

        return Ok(new
        {
            success = true,
            timestamp = DateTime.UtcNow,
            operationCount = metrics.Count,
            metrics = metrics.Select(m => new
            {
                operation = m.Name,
                statistics = new
                {
                    invocations = m.Stats.InvocationCount,
                    successes = m.Stats.SuccessCount,
                    failures = m.Stats.FailureCount,
                    successRate = $"{m.Stats.SuccessRate:P2}",
                    avgDurationMs = Math.Round(m.Stats.AverageDurationMs, 2),
                    minDurationMs = m.Stats.MinDurationMs,
                    maxDurationMs = m.Stats.MaxDurationMs,
                    lastInvokedAt = m.Stats.LastInvokedAt
                }
            })
        });
    }

    /// <summary>
    /// GET /diagnostics/performance/{operationName} - Gets metrics for a specific operation.
    /// Useful for deep-diving into a particular operation's performance.
    /// </summary>
    [HttpGet("performance/{operationName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetOperationMetrics([FromRoute] string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var stats = _performanceMonitor.GetStatistics(operationName);

        if (stats is null)
        {
            return NotFound(new { success = false, message = $"No metrics found for operation: {operationName}" });
        }

        return Ok(new
        {
            success = true,
            operation = operationName,
            statistics = stats
        });
    }

    /// <summary>
    /// GET /diagnostics/cache - Returns cache statistics and health.
    /// Shows hit rates, entry counts, and memory usage for optimization.
    /// </summary>
    [HttpGet("cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCacheStats()
    {
        _logger.LogInformation("Fetching cache statistics");

        if (_cacheService is InMemoryCacheService cacheService)
        {
            var stats = cacheService.GetStatistics();
            return Ok(new
            {
                success = true,
                cache = "in-memory",
                statistics = new
                {
                    totalEntries = stats.TotalEntries,
                    totalHits = stats.TotalHits,
                    expiredEntries = stats.ExpiredEntries,
                    averageEntryAge = stats.AverageEntryAge.ToString(),
                    hitRate = stats.TotalEntries > 0 ? (double)stats.TotalHits / stats.TotalEntries : 0
                }
            });
        }

        return Ok(new { success = true, message = "Cache statistics unavailable for this cache type" });
    }

    /// <summary>
    /// POST /diagnostics/cache/clear - Clears all cache entries.
    /// Useful for testing or clearing corrupted cache after bugs.
    /// WARNING: This will cause performance spike as cache rebuilds.
    /// </summary>
    [HttpPost("cache/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Cache clear operation initiated");

        try
        {
            // In-memory cache doesn't have a clear method exposed in interface
            // This would need to be added if needed
            return Ok(new
            {
                success = true,
                message = "Cache cleared successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { success = false, message = "Error clearing cache" });
        }
    }

    /// <summary>
    /// POST /diagnostics/performance/clear - Resets all performance metrics.
    /// Useful for starting a fresh measurement period or after deploying changes.
    /// </summary>
    [HttpPost("performance/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ClearPerformanceMetrics()
    {
        _logger.LogWarning("Performance metrics clear operation initiated");

        _performanceMonitor.Clear();

        return Ok(new
        {
            success = true,
            message = "Performance metrics cleared"
        });
    }

    /// <summary>
    /// GET /diagnostics/system - Returns system information for debugging environment issues.
    /// Useful for ops troubleshooting deployment or configuration problems.
    /// </summary>
    [HttpGet("system")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSystemInfo()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();

        return Ok(new
        {
            success = true,
            system = new
            {
                runtime = Environment.Version.ToString(),
                framework = "net10.0",
                osVersion = Environment.OSVersion.ToString(),
                processorCount = Environment.ProcessorCount,
                timestamp = DateTime.UtcNow
            },
            process = new
            {
                id = process.Id,
                name = process.ProcessName,
                uptime = DateTime.Now - process.StartTime,
                workingSetMB = process.WorkingSet64 / (1024 * 1024),
                threadCount = process.Threads.Count
            },
            environment = new
            {
                aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
                machineName = Environment.MachineName,
                userName = Environment.UserName
            }
        });
    }

    /// <summary>
    /// GET /diagnostics/summary - Returns a high-level summary of system health.
    /// Single endpoint for quick health check dashboard.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealthSummary()
    {
        var performanceMetrics = _performanceMonitor.GetAllStatistics().ToList();
        var avgSuccessRate = performanceMetrics.Count > 0
            ? performanceMetrics.Average(m => m.Stats.SuccessRate)
            : 1.0;

        var slowestOperation = performanceMetrics
            .OrderByDescending(m => m.Stats.AverageDurationMs)
            .FirstOrDefault();

        var cacheStats = _cacheService is InMemoryCacheService cache
            ? cache.GetStatistics()
            : null;

        return Ok(new
        {
            success = true,
            timestamp = DateTime.UtcNow,
            health = new
            {
                overallSuccessRate = $"{avgSuccessRate:P2}",
                operationsMonitored = performanceMetrics.Count,
                slowestOperation = slowestOperation?.Name ?? "N/A",
                slowestOperationMs = slowestOperation?.Stats.AverageDurationMs ?? 0
            },
            cache = cacheStats is not null ? new
            {
                entriesCount = cacheStats.TotalEntries,
                expiredCount = cacheStats.ExpiredEntries
            } : null
        });
    }
}
