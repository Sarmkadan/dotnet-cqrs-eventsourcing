using Xunit;
using Infrastructure.Middleware;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System;

namespace DotNetCqrsEventSourcing.Infrastructure.Middleware.Tests
{
    public class RateLimitingMiddlewareTests
    {
        [Fact]
        public async Task RequestsUnderLimitPassThrough()
        {
            // Arrange
            var middleware = new RateLimitingMiddleware(new TokenBucket(10, 1));
            var next = new MockMiddleware();
            var request = new HttpRequestMessage(HttpMethod.Get, "/test");
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            var result = await middleware.InvokeAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
        }

        [Fact]
        public async Task RequestOverLimitGetsRejectionStatusCode()
        {
            // Arrange
            var middleware = new RateLimitingMiddleware(new TokenBucket(1, 1));
            var next = new MockMiddleware();
            var request = new HttpRequestMessage(HttpMethod.Get, "/test");
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

            // Act
            var result = await middleware.InvokeAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
        }

        [Fact]
        public async Task WindowResetAllowsRequestsAgain()
        {
            // Arrange
            var middleware = new RateLimitingMiddleware(new TokenBucket(1, 1));
            var next = new MockMiddleware();
            var request = new HttpRequestMessage(HttpMethod.Get, "/test");
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            await middleware.InvokeAsync(request, next, CancellationToken.None);
            await Task.Delay(1000); // wait for 1 second
            var result = await middleware.InvokeAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
        }

        [Fact]
        public async Task DistinctClientKeysAreLimitedIndependently()
        {
            // Arrange
            var middleware = new RateLimitingMiddleware(new TokenBucket(1, 1));
            var next = new MockMiddleware();
            var request1 = new HttpRequestMessage(HttpMethod.Get, "/test1");
            var request2 = new HttpRequestMessage(HttpMethod.Get, "/test2");
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            await middleware.InvokeAsync(request1, next, CancellationToken.None);
            var result1 = await middleware.InvokeAsync(request1, next, CancellationToken.None);
            var result2 = await middleware.InvokeAsync(request2, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result1);
            Assert.Equal(response, result2);
        }
    }
}
