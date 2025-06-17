using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Middleware
{
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static ErrorHandlingMiddleware WithCustomErrorId(this ErrorHandlingMiddleware middleware, string errorId)
        {
            middleware.ErrorId = errorId;
            return middleware;
        }

        public static ErrorHandlingMiddleware WithTimestamp(this ErrorHandlingMiddleware middleware, DateTime timestamp)
        {
            middleware.Timestamp = timestamp;
            return middleware;
        }

        public static void LogError(this ErrorHandlingMiddleware middleware, ILogger logger)
        {
            logger.LogError(
                "ErrorId: {ErrorId}, Message: {Message}, Timestamp: {Timestamp}, Details: {Details}",
                middleware.ErrorId,
                middleware.Message,
                middleware.Timestamp,
                string.Join(", ", middleware.Details));
        }

        public static ErrorHandlingMiddleware AddDetail(this ErrorHandlingMiddleware middleware, string detail)
        {
            middleware.Details = middleware.Details.Concat(new[] { detail }).ToArray();
            return middleware;
        }
    }
}
