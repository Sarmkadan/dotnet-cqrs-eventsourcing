using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    public class IdempotencyMiddleware
    {
        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
        {
            // TODO: implement idempotency middleware
            return await next(request, cancellationToken);
        }
    }
}
