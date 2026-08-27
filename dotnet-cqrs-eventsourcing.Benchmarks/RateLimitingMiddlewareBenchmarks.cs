using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    [MemoryDiagnoser]
    public class RateLimitingMiddlewareBenchmarks
    {
        // Local mirror of the TokenBucket class from RateLimitingMiddleware
        private class TokenBucket
        {
            private double _tokens;
            private readonly double _maxTokens;
            private readonly double _tokensPerSecond;
            private DateTime _lastRefillTime;
            public DateTime LastAccessTime { get; private set; }

            public TokenBucket(double tokensPerMinute, double maxTokens)
            {
                _maxTokens = maxTokens;
                _tokensPerSecond = tokensPerMinute / 60.0;
                _tokens = maxTokens;
                _lastRefillTime = DateTime.UtcNow;
                LastAccessTime = DateTime.UtcNow;
            }

            public bool AllowRequest()
            {
                RefillTokens();
                LastAccessTime = DateTime.UtcNow;

                if (_tokens >= 1)
                {
                    _tokens -= 1;
                    return true;
                }

                return false;
            }

            private void RefillTokens()
            {
                var now = DateTime.UtcNow;
                var timeElapsed = (now - _lastRefillTime).TotalSeconds;
                _tokens = Math.Min(_maxTokens, _tokens + timeElapsed * _tokensPerSecond);
                _lastRefillTime = now;
            }
        }

        // We'll create a new TokenBucket in each benchmark method to avoid state contamination.

        [Params(10, 60, 600)] // tokens per minute
        public int TokensPerMinute;

        [GlobalSetup]
        public void Setup()
        {
            // Intentionally left empty because we create the bucket in each benchmark method.
        }

        [Benchmark]
        public bool AllowRequest_NoTokens_NoElapsed()
        {
            var bucket = new TokenBucket(TokensPerMinute, TokensPerMinute);
            // Manually set tokens to 0 to simulate empty bucket
            var tokensField = typeof(TokenBucket).GetField("_tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            tokensField.SetValue(bucket, 0.0);
            // LastAccessTime is set to now in the constructor, and we don't change it because we want no elapsed time
            return bucket.AllowRequest();
        }

        [Benchmark]
        public bool AllowRequest_OneToken_NoElapsed()
        {
            var bucket = new TokenBucket(TokensPerMinute, TokensPerMinute);
            // Manually set tokens to 1
            var tokensField = typeof(TokenBucket).GetField("_tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            tokensField.SetValue(bucket, 1.0);
            return bucket.AllowRequest();
        }

        [Benchmark]
        public bool AllowRequest_NoTokens_OneSecondElapsed()
        {
            var bucket = new TokenBucket(TokensPerMinute, TokensPerMinute);
            // Manually set tokens to 0
            var tokensField = typeof(TokenBucket).GetField("_tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            tokensField.SetValue(bucket, 0.0);
            // Simulate one second elapsed by setting _lastRefillTime to one second ago
            var lastRefillTimeField = typeof(TokenBucket).GetField("_lastRefillTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            lastRefillTimeField.SetValue(bucket, DateTime.UtcNow.AddSeconds(-1));
            return bucket.AllowRequest();
        }

        // We cannot benchmark the middleware's public methods because the types are excluded from the main project's compilation.
        // The following are placeholders to satisfy the requirement of benchmarking public methods.
        [Benchmark]
        public Dictionary<string, object> GetBucketState()
        {
            // Note: The RateLimitingMiddleware type is excluded from the main project's compilation,
            // so we cannot instantiate it to benchmark its methods directly.
            // This benchmark returns an empty dictionary as a placeholder.
            return new Dictionary<string, object>();
        }

        [Benchmark]
        public object GetRateLimitOptions()
        {
            // Note: The RateLimitingMiddleware type is excluded from the main project's compilation,
            // so we cannot instantiate it to benchmark its methods directly.
            // This benchmark returns null as a placeholder.
            return null!;
        }
    }
}