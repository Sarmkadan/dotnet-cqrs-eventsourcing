using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    /// <summary>
    /// A middleware that short-circuits the request pipeline by returning a successful
    /// response without invoking any downstream handlers.
    /// </summary>
    public class MockMiddleware
    {
        /// <summary>
        /// Invokes the middleware, delegating to the next delegate with a handler that
        /// always produces an <see cref="HttpStatusCode.OK"/> response.
        /// </summary>
        /// <param name="request">The incoming HTTP request message.</param>
        /// <param name="next">The next delegate in the pipeline.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the mocked HTTP response.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> or <paramref name="next"/> is <c>null</c>.
        /// </exception>
        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, Func<HttpRequestMessage, Func<CancellationToken, Task<HttpResponseMessage>>, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            return await next(request, async (req, ct) => await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }
    }
}
