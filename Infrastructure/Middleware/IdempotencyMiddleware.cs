using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    /// <summary>
    /// Provides middleware for processing HTTP requests through an idempotency pipeline.
    /// </summary>
    public class IdempotencyMiddleware
    {
        /// <summary>
        /// Processes an HTTP request by invoking the next delegate in the middleware pipeline.
        /// </summary>
        /// <param name="request">The HTTP request to process.</param>
        /// <param name="next">The delegate that invokes the next middleware component.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation and contains the HTTP response.</returns>
        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
        {
            // TODO: implement idempotency middleware
            return await next(request, cancellationToken);
        }
    }
}
