using Microsoft.AspNetCore.Builder;
using System;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    /// <summary>
    /// Provides extension methods for configuring, querying, and disabling <see cref="RateLimitingMiddleware"/> instances.
    /// These methods enable a fluent API for managing rate‑limiting behavior at runtime.
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        /// <summary>
        /// Configures the rate‑limiting parameters for the specified middleware instance.
        /// The method sets the maximum number of request tokens that can be consumed per minute
        /// and enables the rate‑limiting functionality.
        /// </summary>
        /// <param name="middleware">The <see cref="RateLimitingMiddleware"/> instance to configure. Cannot be null.</param>
        /// <param name="tokensPerMinute">The maximum number of request tokens allowed each minute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static void ConfigureRateLimiting(this RateLimitingMiddleware middleware, double tokensPerMinute)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            var options = middleware.GetRateLimitOptions();
            options.TokensPerMinute = tokensPerMinute;
            options.Enabled = true;
        }

        /// <summary>
        /// Determines whether rate limiting is currently active and permitting requests.
        /// </summary>
        /// <param name="middleware">The <see cref="RateLimitingMiddleware"/> instance to inspect. Cannot be null.</param>
        /// <returns>
        /// <c>true</c> if rate limiting is enabled (<see cref="RateLimitOptions.Enabled"/> is <c>true</c>) 
        /// and the middleware's <see cref="RateLimitingMiddleware.AllowRequest"/> method returns <c>true</c>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static bool IsRateLimitingActive(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            return middleware.GetRateLimitOptions().Enabled && middleware.AllowRequest();
        }

        /// <summary>
        /// Generates a formatted string that describes the current rate‑limiting configuration and status.
        /// The string includes whether rate limiting is enabled, the configured tokens per minute,
        /// and the timestamp of the last request processed by the middleware.
        /// </summary>
        /// <param name="middleware">The <see cref="RateLimitingMiddleware"/> instance to query. Cannot be null.</param>
        /// <returns>
        /// A string in the form 
        /// <c>"Enabled: {Enabled}, TokensPerMinute: {TokensPerMinute}, LastAccess: {yyyy-MM-dd HH:mm:ss}"</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static string GetRateLimitStatus(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            var options = middleware.GetRateLimitOptions();
            return $"Enabled: {options.Enabled}, TokensPerMinute: {options.TokensPerMinute}, " +
                   $"LastAccess: {middleware.LastAccessTime:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Disables rate limiting for the specified middleware instance by setting the <c>Enabled</c> flag to <c>false</c>.
        /// </summary>
        /// <param name="middleware">The <see cref="RateLimitingMiddleware"/> instance to disable. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static void DisableRateLimiting(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            middleware.GetRateLimitOptions().Enabled = false;
        }
    }
}
