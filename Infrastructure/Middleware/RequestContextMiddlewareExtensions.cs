using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace YourProjectNamespace.Infrastructure.Middleware
{
    public static class RequestContextMiddlewareExtensions
    {
        /// <summary>
        /// Adds the RequestContextMiddleware to the pipeline with default configuration.
        /// </summary>
        /// <param name="app">The IApplicationBuilder instance</param>
        /// <returns>The IApplicationBuilder instance for chaining</returns>
        public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestContextMiddleware>();
        }

        /// <summary>
        /// Adds the RequestContextMiddleware to the pipeline with custom correlation ID header configuration.
        /// </summary>
        /// <param name="app">The IApplicationBuilder instance</param>
        /// <param name="correlationIdHeader">The header name to extract correlation ID from (default: X-Correlation-ID)</param>
        /// <returns>The IApplicationBuilder instance for chaining</returns>
        public static IApplicationBuilder UseRequestContext(
            this IApplicationBuilder app,
            string correlationIdHeader)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrWhiteSpace(correlationIdHeader))
            {
                throw new ArgumentException("Correlation ID header name cannot be null or whitespace", nameof(correlationIdHeader));
            }

            return app.UseMiddleware<RequestContextMiddleware>(correlationIdHeader);
        }

        /// <summary>
        /// Gets the current request context information from HttpContext items.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The RequestContextInfo if available, null otherwise</returns>
        public static RequestContextInfo? GetRequestContext(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return RequestContextMiddleware.GetContext(context);
        }

        /// <summary>
        /// Gets the correlation ID from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The correlation ID string</returns>
        public static string GetCorrelationId(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return RequestContextMiddleware.GetCorrelationId(context);
        }

        /// <summary>
        /// Gets the request ID from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The request ID string</returns>
        public static string GetRequestId(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return RequestContextMiddleware.GetRequestId(context);
        }

        /// <summary>
        /// Gets the user ID from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The user ID string if available, null otherwise</returns>
        public static string? GetUserId(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return RequestContextMiddleware.GetUserId(context);
        }

        /// <summary>
        /// Gets the timestamp from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The timestamp when the request context was created</returns>
        public static DateTime GetRequestTimestamp(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the HTTP method from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The HTTP method (GET, POST, PUT, etc.)</returns>
        public static string GetRequestMethod(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Method ?? context.Request.Method;
        }

        /// <summary>
        /// Gets the request path from the current request context.
        /// </summary>
        /// <param name="context">The HttpContext instance</param>
        /// <returns>The request path</returns>
        public static string GetRequestPath(this HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contextInfo = RequestContextMiddleware.GetContext(context);
            return contextInfo?.Path ?? context.Request.Path.Value ?? "/";
        }
    }
}