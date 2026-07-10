using Microsoft.AspNetCore.Builder;
using System;

namespace Infrastructure.Middleware
{
    public static class RateLimitingMiddlewareExtensions
    {
        public static void ConfigureRateLimiting(this RateLimitingMiddleware middleware, double tokensPerMinute)
        {
            middleware.TokensPerMinute = tokensPerMinute;
            middleware.Enabled = true;
        }

        public static bool IsRateLimitingActive(this RateLimitingMiddleware middleware)
        {
            return middleware.Enabled && middleware.AllowRequest;
        }

        public static string GetRateLimitStatus(this RateLimitingMiddleware middleware)
        {
            return $"Enabled: {middleware.Enabled}, TokensPerMinute: {middleware.TokensPerMinute}, LastAccess: {middleware.LastAccessTime}";
        }

        public static void DisableRateLimiting(this RateLimitingMiddleware middleware)
        {
            middleware.Enabled = false;
        }
    }
}
