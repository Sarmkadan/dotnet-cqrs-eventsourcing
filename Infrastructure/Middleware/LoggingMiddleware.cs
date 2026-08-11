        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("LoggingMiddleware initialized");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for health checks and static files to reduce log noise
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestBody = await ReadRequestBodyAsync(context.Request);

            // Reset stream for downstream middleware to read
            context.Request.Body.Position = 0;

            var originalResponseBody = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Read response body (being careful not to disrupt the original stream)
                var responseBody = responseBodyStream.ToArray();
                await originalResponseBody.WriteAsync(responseBody);
                context.Response.Body = originalResponseBody;

                _logger.LogInformation("Request {Method} {Path} completed in {ElapsedMs}ms", context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("Request body: {RequestBody}", requestBody);
                _logger.LogInformation("Response body: {ResponseBody}", Encoding.UTF8.GetString(responseBody));
                _logger.LogInformation("Response status code: {StatusCode}", context.Response.StatusCode);
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            if (request.Body.CanSeek)
            {
                var buffer = new byte[Math.Min(request.ContentLength ?? 0, MaxBodySize)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            }

            return "[Stream not seekable]";
        }

        private void LogRequest(
            HttpContext context,
            long elapsedMs,
            string requestBody,
            byte[] responseBody)
        {
            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Information;

            _logger.LogInformation("HTTP {Method} {Path} - {StatusCode} ({ElapsedMs}ms) | Request: {RequestBody}", context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs, string.IsNullOrWhiteSpace(requestBody) ? "[empty]" : requestBody[..Math.Min(100, requestBody.Length)]);
            _logger.LogInformation("Response body: {ResponseBody}", Encoding.UTF8.GetString(responseBody));
        }

        private static bool ShouldSkipLogging(PathString path)
        {
            var pathStr = path.Value?.ToLower() ?? string.Empty;
            return pathStr.Contains("/health") ||
                   pathStr.Contains("/metrics") ||
                   pathStr.Contains("/swagger") ||
                   pathStr.StartsWith("/wwwroot") ||
                   pathStr.EndsWith(".js") ||
                   pathStr.EndsWith(".css");
        }
