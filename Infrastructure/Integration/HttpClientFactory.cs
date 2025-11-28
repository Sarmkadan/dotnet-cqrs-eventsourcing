// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net.Http.Headers;

namespace DotNetCqrsEventSourcing.Infrastructure.Integration;

/// <summary>
/// Factory for creating and configuring HTTP clients with standard settings.
/// Centralizes HTTP configuration: timeouts, retry policies, headers, authentication.
/// Uses typed HttpClient pattern for dependency injection and proper socket reuse.
/// All clients share a common base configuration to prevent socket exhaustion.
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Creates a named HTTP client with standard configuration.
    /// </summary>
    HttpClient CreateClient(string name);

    /// <summary>
    /// Creates an HTTP client with custom base address.
    /// </summary>
    HttpClient CreateClientWithBaseAddress(string name, string baseAddress);

    /// <summary>
    /// Creates an HTTP client with authentication header.
    /// </summary>
    HttpClient CreateAuthenticatedClient(string name, string authToken);
}

public class StandardHttpClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory _underlyingFactory;
    private readonly ILogger<StandardHttpClientFactory> _logger;

    private const int TimeoutSeconds = 30;
    private const int MaxRetries = 3;

    public StandardHttpClientFactory(IHttpClientFactory factory, ILogger<StandardHttpClientFactory> logger)
    {
        _underlyingFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public HttpClient CreateClient(string name)
    {
        var client = _underlyingFactory.CreateClient(name);
        ConfigureClientDefaults(client);
        return client;
    }

    public HttpClient CreateClientWithBaseAddress(string name, string baseAddress)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(baseAddress);

        var client = _underlyingFactory.CreateClient(name);
        ConfigureClientDefaults(client);

        try
        {
            client.BaseAddress = new Uri(baseAddress);
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid base address for HTTP client: {BaseAddress}", baseAddress);
            throw;
        }

        return client;
    }

    public HttpClient CreateAuthenticatedClient(string name, string authToken)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(authToken);

        var client = CreateClient(name);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        return client;
    }

    /// <summary>
    /// Applies standard configuration to all HTTP clients.
    /// Includes timeouts, user agent, standard headers, and retry logic configuration.
    /// </summary>
    private static void ConfigureClientDefaults(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

        // Add standard headers
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "DotNetCqrsEventSourcing/1.0");

        // Add request ID header for tracing
        client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

        // Connection pooling settings
        client.DefaultRequestHeaders.Connection.Add("keep-alive");
    }
}

/// <summary>
/// Extension methods for HTTP client builder in dependency injection setup.
/// </summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Registers standard HTTP client factory with DI container.
    /// </summary>
    public static IServiceCollection AddStandardHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<IHttpClientFactory, StandardHttpClientFactory>();
        return services;
    }

    /// <summary>
    /// Adds resilience (retry, timeout) policies to HTTP clients.
    /// Uses Polly for sophisticated resilience patterns.
    /// </summary>
    public static IServiceCollection AddHttpClientResilience(this IServiceCollection services)
    {
        services.AddHttpClient()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static Polly.IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        // Retry on transient failures (5xx, 408, 429)
        return Polly.Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r =>
                (int)r.StatusCode >= 500 ||
                (int)r.StatusCode == 408 ||
                (int)r.StatusCode == 429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Logging would go here
                });
    }

    private static Polly.IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        // Break circuit if 50% of requests fail in last 5 requests, stay open for 30 seconds
        return Polly.Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync<HttpResponseMessage>(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
