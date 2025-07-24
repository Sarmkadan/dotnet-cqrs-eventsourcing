using Microsoft.AspNetCore.Builder;
using System;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    /// <summary>
    /// Extension methods for configuring and managing <see cref="RateLimitingMiddleware"/> instances.
    /// Provides fluent API for enabling, disabling, and configuring rate limiting behavior.
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        /// <summary>
        /// Configures the rate limiting parameters for the middleware instance.
        /// </summary>
        /// <param name="middleware">The middleware instance to configure. Cannot be null.</param>
        /// <param name="tokensPerMinute">The maximum number of tokens (requests) allowed per minute.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static void ConfigureRateLimiting(this RateLimitingMiddleware middleware, double tokensPerMinute)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            var options = middleware.GetRateLimitOptions();
            options.TokensPerMinute = tokensPerMinute;
            options.Enabled = true;
        }

        /// <summary>
        /// Determines whether rate limiting is currently active and allowing requests.
        /// </summary>
        /// <param name="middleware">The middleware instance to check. Cannot be null.</param>
        /// <returns>True if rate limiting is enabled and the middleware is allowing requests; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static bool IsRateLimitingActive(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            return middleware.GetRateLimitOptions().Enabled && middleware.AllowRequest();
        }

        /// <summary>
        /// Gets a formatted string representing the current rate limiting configuration and status.
        /// Useful for monitoring and diagnostic purposes.
        /// </summary>
        /// <param name="middleware">The middleware instance to inspect. Cannot be null.</param>
        /// <returns>A formatted string containing the rate limit configuration and status.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static string GetRateLimitStatus(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            var options = middleware.GetRateLimitOptions();
            return $"Enabled: {options.Enabled}, TokensPerMinute: {options.TokensPerMinute}, " +
                   $"LastAccess: {middleware.LastAccessTime:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Disables rate limiting for the middleware instance.
        /// </summary>
        /// <param name="middleware">The middleware instance to disable. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
        public static void DisableRateLimiting(this RateLimitingMiddleware middleware)
        {
            ArgumentNullException.ThrowIfNull(middleware);

            middleware.GetRateLimitOptions().Enabled = false;
        }
    }
}