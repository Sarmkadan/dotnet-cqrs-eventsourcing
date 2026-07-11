using Microsoft.AspNetCore.Http;

namespace Infrastructure.Middleware
{
    /// <summary>
    /// Extension methods for configuring and using ErrorHandlingMiddleware in the request pipeline.
    /// </summary>
    public static class ErrorHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Adds global error handling middleware to the request pipeline.
        /// This should be registered as the first middleware to catch all exceptions.
        /// </summary>
        /// <param name="builder">The application builder</param>
        /// <returns>The configured application builder</returns>
        /// <exception cref="ArgumentNullException">Thrown if builder is null</exception>
        public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}