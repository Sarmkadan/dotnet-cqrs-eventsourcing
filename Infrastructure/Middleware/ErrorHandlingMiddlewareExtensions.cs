using Microsoft.AspNetCore.Http;

namespace Infrastructure.Middleware
{
    /// <summary>
    /// Provides extension methods for integrating global error handling middleware into the ASP.NET request pipeline.
    /// These extensions ensure that unhandled exceptions are captured early in the pipeline.
    /// </summary>
    public static class ErrorHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="ErrorHandlingMiddleware"/> to the request pipeline as the first middleware.
        /// This ensures that all unhandled exceptions are captured before being processed by downstream middleware.
        /// </summary>
        /// <param name="builder">The application builder used to configure the middleware pipeline.</param>
        /// <returns>
        /// The same <see cref="IApplicationBuilder"/> instance to allow for fluent configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is <see langword="null"/>, as the builder is required to configure the pipeline.
        /// </exception>
        public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
