// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// HTTP API for query operations (read models, reports, searches).
/// Implements the Query side of CQRS pattern.
/// Queries are read-only and return current state from projections (eventually consistent).
/// Multiple queries can run in parallel; results may be stale if projections lag behind events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class QueriesController : BaseApiController
{
    private readonly IProjectionService _projectionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<QueriesController> _logger;

    private const string CacheKeyPrefix = "query:";

    public QueriesController(
        IProjectionService projectionService,
        ICacheService cacheService,
        ILogger<QueriesController> logger)
    {
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /queries/accounts - Lists all accounts with pagination.
    /// Results are cached for performance; use force=true to bypass cache.
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAccounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing accounts - page: {Page}, size: {PageSize}", page, pageSize);

        var (validPage, validPageSize) = PaginationHelper.ValidatePaginationParams(page, pageSize);
        var cacheKey = $"{CacheKeyPrefix}accounts:page:{validPage}:size:{validPageSize}";

        // Try to get from cache first
        if (!force)
        {
            var cached = await _cacheService.GetAsync<object>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Serving accounts from cache");
                return Ok(cached);
            }
        }

        try
        {
            // In a real implementation, this would query the projection store
            // For now, return a placeholder that demonstrates the pattern
            var response = new
            {
                success = true,
                data = new object[] { },
                pagination = new
                {
                    pageNumber = validPage,
                    pageSize = validPageSize,
                    totalCount = 0,
                    totalPages = 0
                }
            };

            // Cache the result for 5 minutes
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing accounts");
            return StatusCode(500, new { success = false, message = "Error retrieving accounts" });
        }
    }

    /// <summary>
    /// GET /queries/accounts/search?term=X - Searches accounts by name or ID.
    /// Full-text search across account projection.
    /// </summary>
    [HttpGet("accounts/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAccounts(
        [FromQuery] string term,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);

        _logger.LogInformation("Searching accounts for term: {Term}", term);

        var cacheKey = $"{CacheKeyPrefix}accounts:search:{term.ToLower()}";

        try
        {
            var cached = await _cacheService.GetAsync<object>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            // In real implementation, would search projection store
            var response = new
            {
                success = true,
                searchTerm = term,
                resultCount = 0,
                results = new object[] { }
            };

            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching accounts");
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /queries/accounts/{id}/balance - Gets current balance for an account.
    /// Highly cached since balance changes are infrequent relative to queries.
    /// </summary>
    [HttpGet("accounts/{id}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var cacheKey = $"{CacheKeyPrefix}accounts:{id}:balance";

        try
        {
            var cached = await _cacheService.GetAsync<object>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Serving account balance from cache for {AccountId}", id);
                return Ok(cached);
            }

            // Query from projection
            var balance = 0m; // Would query projection store

            var response = new
            {
                success = true,
                accountId = id,
                balance = balance,
                currency = "USD",
                asOf = DateTime.UtcNow
            };

            // Cache for 1 minute
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(1), cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance for account {AccountId}", id);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /queries/accounts/{id}/transactions - Gets transaction history for an account.
    /// Supports filtering by date range and pagination.
    /// </summary>
    [HttpGet("accounts/{id}/transactions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionHistory(
        [FromRoute] string id,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogInformation(
            "Querying transactions for account {AccountId} from {FromDate} to {ToDate}",
            id,
            fromDate?.ToShortDateString() ?? "beginning",
            toDate?.ToShortDateString() ?? "today"
        );

        var (validPage, validPageSize) = PaginationHelper.ValidatePaginationParams(page, pageSize);

        try
        {
            // In real implementation, would query projection with date filter
            var response = new
            {
                success = true,
                accountId = id,
                dateRange = new
                {
                    from = fromDate?.ToShortDateString() ?? "earliest",
                    to = toDate?.ToShortDateString() ?? "latest"
                },
                pagination = new
                {
                    pageNumber = validPage,
                    pageSize = validPageSize,
                    totalCount = 0,
                    totalPages = 0
                },
                transactions = new object[] { }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying transactions for account {AccountId}", id);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /queries/statistics/accounts - Gets aggregate statistics about accounts.
    /// Shows total accounts, average balance, transaction volume, etc.
    /// Useful for dashboards and reporting.
    /// </summary>
    [HttpGet("statistics/accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountStatistics(CancellationToken cancellationToken = default)
    {
        const string cacheKey = $"{CacheKeyPrefix}statistics:accounts";

        try
        {
            var cached = await _cacheService.GetAsync<object>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Ok(cached);
            }

            var response = new
            {
                success = true,
                statistics = new
                {
                    totalAccounts = 0,
                    activeAccounts = 0,
                    totalBalance = 0m,
                    averageBalance = 0m,
                    totalTransactions = 0,
                    asOf = DateTime.UtcNow
                }
            };

            // Cache for 30 minutes
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(30), cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account statistics");
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// GET /queries/cache/invalidate - Admin endpoint to invalidate query cache.
    /// Use after deploying projection changes or fixing data corruption.
    /// </summary>
    [HttpPost("cache/invalidate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateQueryCache(
        [FromQuery] string? pattern = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Query cache invalidation initiated - pattern: {Pattern}", pattern ?? "*");

        try
        {
            var invalidatePattern = pattern ?? $"{CacheKeyPrefix}*";
            await _cacheService.RemoveByPatternAsync(invalidatePattern, cancellationToken);

            return Ok(new
            {
                success = true,
                message = $"Cache invalidated for pattern: {invalidatePattern}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache");
            return StatusCode(500, new { success = false });
        }
    }
}
