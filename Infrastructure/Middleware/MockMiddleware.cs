using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    public class MockMiddleware
    {
        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, Func<HttpRequestMessage, Func<CancellationToken, Task<HttpResponseMessage>>, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (next == null)
                throw new ArgumentNullException(nameof(next));
            return await next(request, async (req, ct) => await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }
        {
            return await next(request, async (req, ct) => await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            // TODO: implement idempotency middleware
            // TODO: implement idempotency middleware        }
    }
}
