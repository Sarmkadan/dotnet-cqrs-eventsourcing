// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace DotNetCqrsEventSourcing.Infrastructure.Idempotency;

/// <summary>
/// Idempotency key handler for ensuring operations are executed at most once.
/// Prevents duplicate processing of requests due to network retries or client mistakes.
/// Stores idempotency keys and their results for a configurable retention period.
/// Thread-safe using concurrent collections; suitable for distributed system scenarios.
/// For production systems, replace with persistent storage (database, Redis).
/// </summary>
public interface IIdempotencyKeyHandler
{
    /// <summary>
    /// Checks if an idempotency key has been processed before.
    /// Returns the previous result if found, null if not processed.
    /// </summary>
    Task<IdempotencyResult?> GetPreviousResultAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that an idempotency key has been processed with the given result.
    /// Subsequent calls with the same key will return this result.
    /// </summary>
    Task RecordResultAsync(string idempotencyKey, IdempotencyResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all stored idempotency keys (useful in tests).
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

public class InMemoryIdempotencyKeyHandler : IIdempotencyKeyHandler
{
    private readonly ConcurrentDictionary<string, StoredIdempotencyResult> _store = new();
    private readonly ILogger<InMemoryIdempotencyKeyHandler> _logger;
    private readonly TimeSpan _retentionPeriod;
    private readonly Timer _cleanupTimer;

    private const int DefaultRetentionHours = 24;

    public InMemoryIdempotencyKeyHandler(
        ILogger<InMemoryIdempotencyKeyHandler> logger,
        TimeSpan? retentionPeriod = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retentionPeriod = retentionPeriod ?? TimeSpan.FromHours(DefaultRetentionHours);

        // Clean up expired entries every 6 hours
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromHours(6), TimeSpan.FromHours(6));
    }

    public Task<IdempotencyResult?> GetPreviousResultAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        if (_store.TryGetValue(idempotencyKey, out var stored))
        {
            // Check if entry has expired
            if (DateTime.UtcNow - stored.RecordedAt > _retentionPeriod)
            {
                _store.TryRemove(idempotencyKey, out _);
                _logger.LogDebug("Idempotency key expired: {IdempotencyKey}", idempotencyKey);
                return Task.FromResult<IdempotencyResult?>(null);
            }

            _logger.LogInformation("Idempotency key found (replay): {IdempotencyKey}", idempotencyKey);
            return Task.FromResult<IdempotencyResult?>(stored.Result);
        }

        return Task.FromResult<IdempotencyResult?>(null);
    }

    public Task RecordResultAsync(string idempotencyKey, IdempotencyResult result, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(result);

        var stored = new StoredIdempotencyResult
        {
            Result = result,
            RecordedAt = DateTime.UtcNow
        };

        _store[idempotencyKey] = stored;
        _logger.LogInformation("Idempotency key recorded: {IdempotencyKey}", idempotencyKey);

        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        _logger.LogInformation("All idempotency keys cleared");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes expired idempotency keys to prevent unbounded memory growth.
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _store
            .Where(kvp => now - kvp.Value.RecordedAt > _retentionPeriod)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _store.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired idempotency keys", expiredKeys.Count);
        }
    }

    private class StoredIdempotencyResult
    {
        public IdempotencyResult Result { get; set; } = null!;
        public DateTime RecordedAt { get; set; }
    }
}

/// <summary>
/// Result of a previously-executed idempotent operation.
/// Returned to client to avoid re-executing the same operation.
/// </summary>
public class IdempotencyResult
{
    /// <summary>
    /// HTTP status code that the original operation returned.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body (serialized as JSON).
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// When this result was originally recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Middleware to handle idempotency key checking and response caching.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyKeyHandler _idempotencyHandler;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IIdempotencyKeyHandler idempotencyHandler,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _idempotencyHandler = idempotencyHandler ?? throw new ArgumentNullException(nameof(idempotencyHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply idempotency to mutation operations
        if (!IsMutationOperation(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValue))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyValue.ToString();

        // Check if this operation was already processed
        var previousResult = await _idempotencyHandler.GetPreviousResultAsync(idempotencyKey);

        if (previousResult is not null)
        {
            _logger.LogInformation("Returning cached result for idempotency key: {IdempotencyKey}", idempotencyKey);

            context.Response.StatusCode = previousResult.StatusCode;
            await context.Response.WriteAsync(previousResult.ResponseBody);
            return;
        }

        // Capture response for future idempotency checks
        var originalBodyStream = context.Response.Body;
        using var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        try
        {
            await _next(context);

            // Read and store the response
            responseStream.Position = 0;
            var responseBody = await new StreamReader(responseStream).ReadToEndAsync();

            var result = new IdempotencyResult
            {
                StatusCode = context.Response.StatusCode,
                ResponseBody = responseBody,
                RecordedAt = DateTime.UtcNow
            };

            await _idempotencyHandler.RecordResultAsync(idempotencyKey, result);

            // Write response to original stream
            await originalBodyStream.WriteAsync(responseStream.ToArray());
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool IsMutationOperation(string method)
    {
        return method is "POST" or "PUT" or "DELETE" or "PATCH";
    }
}

public static class IdempotencyExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }

    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyKeyHandler, InMemoryIdempotencyKeyHandler>();
        return services;
    }
}
