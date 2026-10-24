#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Health;

/// <summary>
/// Collects the health of every registered <see cref="IHealthContributor"/> into a single <see cref="HealthSnapshot"/>.
/// A contributor that throws does not abort the collection; it is reported as an
/// <see cref="HealthState.Unhealthy"/> component so the failure stays visible.
/// </summary>
public sealed class HealthAggregator
{
    private readonly IReadOnlyList<IHealthContributor> _contributors;
    private readonly ILogger<HealthAggregator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthAggregator"/> class.
    /// </summary>
    /// <param name="contributors">Components that report their health.</param>
    /// <param name="logger">Logger used to record contributor failures.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contributors"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public HealthAggregator(IEnumerable<IHealthContributor> contributors, ILogger<HealthAggregator> logger)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(logger);

        _contributors = contributors.ToList();
        _logger = logger;
    }

    /// <summary>
    /// Queries every contributor in registration order and returns the combined snapshot.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the collection.</param>
    /// <returns>A snapshot containing the components reported by all contributors.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled during collection.</exception>
    public async Task<HealthSnapshot> CollectAsync(CancellationToken cancellationToken = default)
    {
        var components = new List<ComponentHealth>();

        foreach (var contributor in _contributors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contributorName = contributor.GetType().Name;
            try
            {
                components.AddRange(await contributor.GetHealthAsync(cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health contributor {Contributor} failed", contributorName);
                components.Add(new ComponentHealth(
                    $"contributor:{contributorName}",
                    HealthState.Unhealthy,
                    $"Health contributor failed: {ex.Message}",
                    new Dictionary<string, long>()));
            }
        }

        return new HealthSnapshot(DateTime.UtcNow, components);
    }
}
