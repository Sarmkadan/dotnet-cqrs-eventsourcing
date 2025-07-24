#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware;

/// <summary>
/// Provides System.Text.Json serialization/deserialization helpers for rate limiting state.
/// Enables serialization of rate limiting configuration and bucket state for debugging, monitoring,
/// and persistence scenarios. Note: Only serializes the configuration and current bucket state,
/// not the middleware instance itself which contains non-serializable dependencies.
/// </summary>
public static class RateLimitingMiddlewareJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serializes the rate limiting configuration and current bucket state to a JSON string.
    /// </summary>
    /// <param name="middleware">The middleware instance containing rate limiting state.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the rate limiting configuration and state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
    public static string ToJson(this RateLimitingMiddleware middleware, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        var state = new RateLimitingState
        {
            Options = middleware.GetRateLimitOptions(),
            BucketState = middleware.GetBucketState()
        };

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(state, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a rate limiting state.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized rate limiting state, or null if JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
    public static RateLimitingState? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RateLimitingState>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a rate limiting state.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="state">The deserialized rate limiting state, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeded; false otherwise.</returns>
    public static bool TryFromJson(string json, out RateLimitingState? state)
    {
        state = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            state = JsonSerializer.Deserialize<RateLimitingState>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Represents the serializable state of rate limiting configuration and bucket data.
    /// </summary>
    private sealed class RateLimitingState
    {
        public RateLimitOptions? Options { get; set; }
        public Dictionary<string, TokenBucketState>? BucketState { get; set; }
    }

    /// <summary>
    /// Represents the serializable state of a token bucket.
    /// </summary>
    private sealed class TokenBucketState
    {
        public double Tokens { get; set; }
        public double MaxTokens { get; set; }
        public double TokensPerSecond { get; set; }
        public DateTime LastRefillTime { get; set; }
        public DateTime LastAccessTime { get; set; }
    }
}