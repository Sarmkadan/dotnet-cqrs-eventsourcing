using System;
using Microsoft.AspNetCore.Http;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    /// <summary>
    /// Extension methods for configuring request context middleware in the ASP.NET Core pipeline.
    /// Provides access to request correlation, tracing, and contextual information throughout the application.
    /// </summary>
    public sealed class RequestContextMiddlewareExtensions
    {
        /// <summary>
        /// Adds the RequestContextMiddleware to the pipeline with default configuration.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/></param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chaining</returns>
        /// <exception cref="ArgumentNullException"><paramref name="app"/> is null</exception>
        public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.UseMiddleware<RequestContextMiddleware>();
        }

        /// <summary>
        /// Adds the RequestContextMiddleware to the pipeline with custom correlation ID header configuration.
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/></param>
        /// <param name="correlationIdHeader">The header name to extract correlation ID from (default: X-Correlation-ID)</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chaining</returns>
        /// <exception cref="ArgumentNullException"><paramref name="app"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="correlationIdHeader"/> is null or whitespace</exception>
        public static IApplicationBuilder UseRequestContext(
            this IApplicationBuilder app,
            string correlationIdHeader)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentException.ThrowIfNullOrWhiteSpace(correlationIdHeader);

            return app.UseMiddleware<RequestContextMiddleware>(correlationIdHeader);
        }

        /// <summary>
        /// Gets the current request context information from HttpContext items.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The <see cref="RequestContextInfo"/> if available, null otherwise</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static RequestContextInfo? GetRequestContext(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return RequestContextMiddleware.GetContext(context);
        }

        /// <summary>
        /// Gets the correlation ID from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The correlation ID string</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static string GetCorrelationId(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return RequestContextMiddleware.GetCorrelationId(context);
        }

        /// <summary>
        /// Gets the request ID from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The request ID string</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static string GetRequestId(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return RequestContextMiddleware.GetRequestId(context);
        }

        /// <summary>
        /// Gets the user ID from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The user ID string if available, null otherwise</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static string? GetUserId(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return RequestContextMiddleware.GetUserId(context);
        }

        /// <summary>
        /// Gets the timestamp from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The timestamp when the request context was created</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static DateTime GetRequestTimestamp(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the HTTP method from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The HTTP method (GET, POST, PUT, etc.)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static string GetRequestMethod(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Method ?? context.Request.Method;
        }

        /// <summary>
        /// Gets the request path from the current request context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> instance</param>
        /// <returns>The request path</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null</exception>
        public static string GetRequestPath(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Path ?? context.Request.Path.Value ?? "/";
        }
    }
}