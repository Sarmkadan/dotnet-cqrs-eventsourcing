        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("LoggingMiddleware initialized with {Next} and {Logger}", next, logger);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("InvokeAsync called with {Context}", context);
            // Skip logging for health checks and static files to reduce log noise
            if (ShouldSkipLogging(context.Request.Path))
            {
                _logger.LogWarning("Skipping logging for {Path}", context.Request.Path);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InvokeAsync with {Context}", context);
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
                _logger.LogInformation("InvokeAsync completed with {Context}", context);
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            _logger.LogInformation("ReadRequestBodyAsync called with {Request}", request);
            try
            {
                if (request.Body.CanSeek)
                {
                    var buffer = new byte[Math.Min(request.ContentLength ?? 0, MaxBodySize)];
                    await request.Body.ReadAsync(buffer, 0, buffer.Length);
                    return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                }

                _logger.LogWarning("Request body stream is not seekable");
                return "[Stream not seekable]";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReadRequestBodyAsync with {Request}", request);
                return "[Error reading request body]";
            }
            finally
            {
                _logger.LogInformation("ReadRequestBodyAsync completed with {Request}", request);
            }
        }

        private void LogRequest(
            HttpContext context,
            long elapsedMs,
            string requestBody,
            byte[] responseBody)
        {
            _logger.LogInformation("LogRequest called with {Context}, {ElapsedMs}, {RequestBody}, and {ResponseBody}", context, elapsedMs, requestBody, responseBody);
            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Information;

            _logger.LogInformation("HTTP {Method} {Path} - {StatusCode} ({ElapsedMs}ms) | Request: {RequestBody}", context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs, string.IsNullOrWhiteSpace(requestBody) ? "[empty]" : requestBody[..Math.Min(100, requestBody.Length)]);
            _logger.LogInformation("Response body: {ResponseBody}", Encoding.UTF8.GetString(responseBody));
            _logger.LogInformation("LogRequest completed with {Context}, {ElapsedMs}, {RequestBody}, and {ResponseBody}", context, elapsedMs, requestBody, responseBody);
        }

        private static bool ShouldSkipLogging(PathString path)
        {
            _logger.LogInformation("ShouldSkipLogging called with {Path}", path);
            try
            {
                var pathStr = path.Value?.ToLower() ?? string.Empty;
                var result = pathStr.Contains("/health") ||
                             pathStr.Contains("/metrics") ||
                             pathStr.Contains("/swagger") ||
                             pathStr.StartsWith("/wwwroot") ||
                             pathStr.EndsWith(".js") ||
                             pathStr.EndsWith(".css");
                _logger.LogInformation("ShouldSkipLogging result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShouldSkipLogging with {Path}", path);
                return true;
            }
            finally
            {
                _logger.LogInformation("ShouldSkipLogging completed with {Path}", path);
            }
        }
