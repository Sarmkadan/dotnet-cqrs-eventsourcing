#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Cli;

using Health;
using Microsoft.Extensions.Logging;
using Shared.Results;

/// <summary>
/// CLI command that prints the aggregated health snapshot: worker liveness, projection lag,
/// projection failure streaks, and unresolved dead-letter depth.
/// <para>
/// The snapshot covers only the current process. The default stores are in-memory, so a host
/// running elsewhere is not visible from this command; run it inside the host to see live state.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// dotnet run -- health-status
/// </code>
/// </para>
/// </summary>
public sealed class HealthStatusCommand : ICliCommand
{
    private readonly HealthAggregator _aggregator;
    private readonly ILogger<HealthStatusCommand> _logger;

    /// <summary>
    /// Gets the name used to invoke this command from the CLI.
    /// </summary>
    public string Name => "health-status";

    /// <summary>
    /// Gets a short description of what this command reports.
    /// </summary>
    public string Description => "Shows worker liveness, projection lag, failure streaks, and dead-letter depth.";

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStatusCommand"/> class.
    /// </summary>
    /// <param name="aggregator">Aggregator that collects the health of all registered components.</param>
    /// <param name="logger">Logger used to record the collected snapshot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="aggregator"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public HealthStatusCommand(HealthAggregator aggregator, ILogger<HealthStatusCommand> logger)
    {
        ArgumentNullException.ThrowIfNull(aggregator);
        ArgumentNullException.ThrowIfNull(logger);

        _aggregator = aggregator;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The command reports state rather than judging it, so it succeeds even when the snapshot is unhealthy.
    /// It fails only when the snapshot cannot be collected.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    public async Task<Result> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return Result.Success();
        }

        try
        {
            var snapshot = await _aggregator.CollectAsync(cancellationToken);
            PrintSnapshot(snapshot);

            _logger.LogInformation(
                "Reported health snapshot {OverallState} across {ComponentCount} component(s)",
                snapshot.State,
                snapshot.Components.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting health snapshot");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return Result.Failure("HEALTH_FAILED", ex.Message);
        }
    }

    /// <inheritdoc/>
    public void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine($"Usage: dotnet run -- {Name}");
        Console.WriteLine();
        Console.WriteLine("Prints each component's state, description and metrics. Exit code is 0 unless collection fails.");
        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static void PrintSnapshot(HealthSnapshot snapshot)
    {
        if (snapshot.Components.Count == 0)
        {
            Console.WriteLine("No health components are registered in this process.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Health: {snapshot.State} (captured {snapshot.CapturedAtUtc:O})");
        Console.WriteLine();

        foreach (var component in snapshot.Components)
        {
            Console.WriteLine($"{component.State,-10} {component.Name}");
            Console.WriteLine($"           {component.Description}");

            foreach (var (metric, value) in component.Metrics)
            {
                Console.WriteLine($"           {metric} = {value}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Scope: this process only. In-memory state is not shared with a separately running host.");
    }
}
