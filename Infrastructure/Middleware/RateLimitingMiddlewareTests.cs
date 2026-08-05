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
    /// <summary>
    /// Contains unit tests for the <see cref="RateLimitingMiddleware"/> class.
    /// </summary>
    public class RateLimitingMiddlewareTests
    {
        /// <summary>
        /// Verifies that a request made while the token bucket has available tokens
        /// passes through the middleware and returns the response from the next component.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the test finishes.</returns>
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

        /// <summary>
        /// Verifies that a request made when the token bucket is empty
        /// is rejected with the <see cref="HttpStatusCode.TooManyRequests"/> status code.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the test finishes.</returns>
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

        /// <summary>
        /// Verifies that after the token bucket refill interval elapses,
        /// the middleware again allows requests to pass through.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the test finishes.</returns>
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

        /// <summary>
        /// Verifies that distinct client request keys are throttled independently,
        /// allowing each client to make a request within its own token bucket limits.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the test finishes.</returns>
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
