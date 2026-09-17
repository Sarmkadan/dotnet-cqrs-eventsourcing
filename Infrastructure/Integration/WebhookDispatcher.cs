#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Infrastructure.Integration;

/// <summary>
/// Dispatches domain events to external systems via HTTP webhooks.
/// Implements retry logic, signature verification, and idempotency keys.
/// Events are queued and processed asynchronously to avoid blocking the main request.
/// Webhook failures are logged but don't affect the event stream.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Registers a webhook endpoint to receive specific domain events.
    /// </summary>
    Task RegisterWebhookAsync(string webhookUrl, Type eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a webhook endpoint.
    /// </summary>
    Task UnregisterWebhookAsync(string webhookUrl, Type eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches an event to all registered webhooks for that event type.
    /// Fires and forgets - failures don't affect the caller.
    /// </summary>
    Task DispatchAsync(DomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of registered webhooks (useful for testing/admin).
    /// </summary>
    Task<IEnumerable<WebhookRegistration>> GetRegistrationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IWebhookDispatcher"/> implementation that delivers domain
/// events to registered HTTP webhook endpoints with retry, signature, and idempotency support.
/// </summary>
public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcher> _logger;
    private readonly List<WebhookRegistration> _registrations = new();
    private readonly object _registrationsLock = new();

    private const int MaxRetries = 3;
    private const int TimeoutSeconds = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDispatcher"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to deliver webhook payloads.</param>
    /// <param name="logger">The logger used to record dispatch activity.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> or <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public WebhookDispatcher(HttpClient httpClient, ILogger<WebhookDispatcher> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }

    /// <summary>
    /// Registers a webhook endpoint to receive events of the specified type.
    /// </summary>
    /// <param name="webhookUrl">The URL of the webhook endpoint to register.</param>
    /// <param name="eventType">The domain event type the webhook should receive.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the webhook has been registered.</returns>
    public Task RegisterWebhookAsync(string webhookUrl, Type eventType, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNullOrEmpty(webhookUrl, nameof(webhookUrl));
        GuardClauses.NotNull(eventType, nameof(eventType));

        lock (_registrationsLock)
        {
            var registration = new WebhookRegistration
            {
                Id = Guid.NewGuid(),
                WebhookUrl = webhookUrl,
                EventType = eventType,
                RegisteredAt = DateTime.UtcNow,
                Active = true
            };

            _registrations.Add(registration);
            _logger.LogInformation(
                "Webhook registered: {Url} for event type {EventType}",
                webhookUrl,
                eventType.Name
            );
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters all webhook endpoints matching the given URL and event type.
    /// </summary>
    /// <param name="webhookUrl">The URL of the webhook endpoint to unregister.</param>
    /// <param name="eventType">The domain event type to unregister.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the matching webhooks have been unregistered.</returns>
    public Task UnregisterWebhookAsync(string webhookUrl, Type eventType, CancellationToken cancellationToken = default)
    {
        lock (_registrationsLock)
        {
            var toRemove = _registrations
                .Where(r => r.WebhookUrl == webhookUrl && r.EventType == eventType)
                .ToList();

            foreach (var registration in toRemove)
            {
                _registrations.Remove(registration);
            }

            _logger.LogInformation(
                "Unregistered {Count} webhooks for {Url}",
                toRemove.Count,
                webhookUrl
            );
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches an event to all active webhooks registered for its type.
    /// Fire and forget - failures don't affect the caller.
    /// </summary>
    /// <param name="event">The domain event to dispatch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the dispatch has been scheduled.</returns>
    public async Task DispatchAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        GuardClauses.NotNull(@event, nameof(@event));

        WebhookRegistration[] registrationsSnapshot;
        lock (_registrationsLock)
        {
            // Get matching registrations and snapshot to avoid lock contention
            registrationsSnapshot = _registrations
                .Where(r => r.Active && r.EventType.IsInstanceOfType(@event))
                .ToArray();
        }

        if (registrationsSnapshot.Length == 0)
        {
            return;
        }

        _logger.LogDebug(
            "Dispatching event {EventType} to {Count} webhooks",
            @event.GetType().Name,
            registrationsSnapshot.Length
        );

        // Dispatch to each webhook asynchronously (don't await)
        _ = Task.Run(async () =>
        {
            foreach (var registration in registrationsSnapshot)
            {
                await DispatchToWebhookAsync(registration, @event, cancellationToken);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets a snapshot of all registered webhooks (useful for testing/admin).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that yields the current webhook registrations.</returns>
    public Task<IEnumerable<WebhookRegistration>> GetRegistrationsAsync(CancellationToken cancellationToken = default)
    {
        lock (_registrationsLock)
        {
            return Task.FromResult<IEnumerable<WebhookRegistration>>(_registrations.ToList());
        }
    }

    /// <summary>
    /// Dispatches an event to a single webhook with retry logic and signature.
    /// </summary>
    private async Task DispatchToWebhookAsync(WebhookRegistration registration, DomainEvent @event, CancellationToken cancellationToken)
    {
        var payload = new
        {
            @event.CorrelationId,
            @event.AggregateId,
            EventType = @event.GetType().Name,
            @event.Timestamp,
            Data = @event
        };

        var json = JsonSerializer.Serialize(payload);
        var signature = ComputeSignature(json);
        var idempotencyKey = @event.CorrelationId;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                content.Headers.Add("X-Webhook-Signature", signature);
                content.Headers.Add("X-Idempotency-Key", idempotencyKey);

                var response = await _httpClient.PostAsync(registration.WebhookUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook dispatched successfully: {Url} [EventType: {EventType}]",
                        registration.WebhookUrl,
                        @event.GetType().Name
                    );
                    return;
                }

                if (!response.StatusCode.ToString().StartsWith("5") && (int)response.StatusCode != 429)
                {
                    // Don't retry on client errors (except 429 rate limit)
                    _logger.LogWarning(
                        "Webhook returned {StatusCode}: {Url}",
                        response.StatusCode,
                        registration.WebhookUrl
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Webhook dispatch failed (attempt {Attempt}/{MaxRetries}): {Url}",
                    attempt + 1,
                    MaxRetries,
                    registration.WebhookUrl
                );
            }

            if (attempt < MaxRetries - 1)
            {
                // Exponential backoff: 1s, 2s, 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError(
            "Webhook dispatch failed after {MaxRetries} attempts: {Url}",
            MaxRetries,
            registration.WebhookUrl
        );
    }

    /// <summary>
    /// Computes HMAC signature for webhook payload verification.
    /// Receiver can verify the signature to ensure the event came from this system.
    /// </summary>
    private static string ComputeSignature(string payload)
    {
        // In production, use a shared secret key
        var secret = "webhook-secret-key";
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret)
        );
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Represents a webhook endpoint registered to receive domain events.
/// </summary>
public sealed class WebhookRegistration
{
    /// <summary>
    /// Gets or sets the unique identifier of the registration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the URL of the webhook endpoint.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain event type this webhook receives.
    /// </summary>
    public Type EventType { get; set; } = typeof(object);

    /// <summary>
    /// Gets or sets the UTC time the webhook was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is active and receiving events.
    /// </summary>
    public bool Active { get; set; }
}
