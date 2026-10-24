#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Infrastructure.Health;

namespace DotNetCqrsEventSourcing.Infrastructure.Workers;

/// <summary>
/// Background worker that periodically collects a <see cref="HealthSnapshot"/> and writes it to the log.
/// Each component produces one state line, and each of its metrics produces one line with the
/// <c>Component</c>, <c>Metric</c> and <c>MetricValue</c> properties, so the values can be queried
/// by a log pipeline. The level is <c>Warning</c> whenever the overall state is not healthy.
/// </summary>
public sealed class HealthReportWorker : BackgroundService
{
    private const int DefaultReportIntervalSeconds = 60;

    private readonly HealthAggregator _aggregator;
    private readonly ILogger<HealthReportWorker> _logger;
    private readonly TimeSpan _reportInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthReportWorker"/> class.
    /// </summary>
    /// <param name="aggregator">Aggregator that collects the health of all contributors.</param>
    /// <param name="logger">Logger used to write the health report.</param>
    /// <param name="reportInterval">Time between reports; defaults to 60 seconds.</param>
    /// <exception cref="ArgumentNullException"><paramref name="aggregator"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reportInterval"/> is zero or negative.</exception>
    public HealthReportWorker(
        HealthAggregator aggregator,
        ILogger<HealthReportWorker> logger,
        TimeSpan? reportInterval = null)
    {
        ArgumentNullException.ThrowIfNull(aggregator);
        ArgumentNullException.ThrowIfNull(logger);

        _reportInterval = reportInterval ?? TimeSpan.FromSeconds(DefaultReportIntervalSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_reportInterval, TimeSpan.Zero);

        _aggregator = aggregator;
        _logger = logger;
    }

    /// <summary>
    /// Writes a health report on every tick of the report interval until the host stops.
    /// A failed report is logged and does not stop the worker.
    /// </summary>
    /// <param name="stoppingToken">Token signalled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_reportInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ReportAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
    }

    private async Task ReportAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _aggregator.CollectAsync(cancellationToken);
            var level = snapshot.State == HealthState.Healthy ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(level, "Health snapshot {OverallState} across {ComponentCount} component(s)", snapshot.State, snapshot.Components.Count);

            foreach (var component in snapshot.Components)
            {
                _logger.Log(
                    component.State == HealthState.Healthy ? LogLevel.Information : LogLevel.Warning,
                    "Health component {Component} is {ComponentState}: {Description}",
                    component.Name,
                    component.State,
                    component.Description);

                foreach (var (metric, value) in component.Metrics)
                {
                    _logger.LogInformation(
                        "Health metric {Component} {Metric}={MetricValue}",
                        component.Name,
                        metric,
                        value);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health report failed");
        }
    }
}
