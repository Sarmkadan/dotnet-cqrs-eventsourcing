using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware
{
    public class MockMiddleware
    {
        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request, Func<HttpRequestMessage, Func<CancellationToken, Task<HttpResponseMessage>>, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
        {
            return await next(request, async (req, ct) => await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }
    }
}
