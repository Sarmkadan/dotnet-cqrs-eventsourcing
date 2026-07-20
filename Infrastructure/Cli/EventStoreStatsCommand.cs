#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Cli;

using Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Results;
using Utilities;

/// <summary>
/// CLI command for displaying statistics about the event store.
/// Shows statistics using the available IEventStore query methods.
/// <para>
/// <b>Usage:</b>
/// <code>
/// dotnet run -- event-store-stats [--aggregate &lt;id&gt;]
/// </code>
/// </para>
/// </summary>
public sealed class EventStoreStatsCommand : ICliCommand
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventStoreStatsCommand> _logger;

    public string Name => "event-store-stats";
    public string Description => "Displays statistics about the event store using available query methods.";

    public EventStoreStatsCommand(
        IEventStore eventStore,
        ILogger<EventStoreStatsCommand> logger)
    {
        _eventStore = GuardClauses.NotNull(eventStore, nameof(eventStore));
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // Check for help flag
        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return Result.Success();
        }

        var aggregateIdIndex = Array.IndexOf(args, "--aggregate");
        string? aggregateId = null;

        if (aggregateIdIndex >= 0)
        {
            if (aggregateIdIndex + 1 >= args.Length)
            {
                PrintUsage();
                return Result.Failure("MISSING_ARGUMENT", "Aggregate ID must be specified after --aggregate");
            }

            aggregateId = args[aggregateIdIndex + 1];

            // Validate aggregate ID is not just another flag
            if (aggregateId.StartsWith("-"))
            {
                PrintUsage();
                return Result.Failure("INVALID_ARGUMENT", "Aggregate ID must be specified after --aggregate");
            }
        }

        try
        {
            if (aggregateId is not null)
            {
                return await GetAggregateStatsAsync(aggregateId, cancellationToken);
            }

            return await GetOverallStatsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event store statistics");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return Result.Failure("STATS_FAILED", ex.Message);
        }
    }

    /// <inheritdoc/>
    public void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine($"Usage: dotnet run -- {Name} [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine(" --aggregate <id> Show statistics for a specific aggregate stream.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine(" dotnet run -- event-store-stats");
        Console.WriteLine(" dotnet run -- event-store-stats --aggregate ACC-001");
        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<Result> GetOverallStatsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Collecting event store statistics...");
        Console.WriteLine();

        // Note: The IEventStore interface doesn't provide direct methods to get all stream IDs
        // or aggregate all statistics. This command demonstrates what CAN be retrieved.
        // For a complete solution, you would need access to stream enumeration methods.

        Console.WriteLine("Event Store Statistics:");
        Console.WriteLine("======================");
        Console.WriteLine("Note: Full aggregate enumeration requires additional infrastructure.");
        Console.WriteLine("This command demonstrates statistics retrieval using available methods.");
        Console.WriteLine();

        _logger.LogInformation("Retrieved basic event store statistics");

        return Result.Success();
    }

    private async Task<Result> GetAggregateStatsAsync(string aggregateId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Collecting statistics for aggregate '{aggregateId}'...");
        Console.WriteLine();

        // Get the event stream for the specified aggregate
        var streamResult = await _eventStore.GetEventStreamAsync(aggregateId, cancellationToken);
        if (!streamResult.IsSuccess)
        {
            Console.WriteLine($"No events found for aggregate '{aggregateId}'");
            return Result.Success();
        }

        var events = streamResult.Data!;

        // Get the current version
        var versionResult = await _eventStore.GetAggregateVersionAsync(aggregateId, cancellationToken);
        var version = versionResult.IsSuccess ? versionResult.Data : 0;

        // Get the event count
        var countResult = await _eventStore.GetEventCountAsync(aggregateId, cancellationToken);
        var eventCount = countResult.IsSuccess ? countResult.Data : events.Count;

        Console.WriteLine("Aggregate Statistics:");
        Console.WriteLine("===================");
        Console.WriteLine($"Aggregate ID: {aggregateId}");
        Console.WriteLine($"Current Version: {version}");
        Console.WriteLine($"Event Count: {eventCount}");
        Console.WriteLine($"Events in Memory: {events.Count}");
        Console.WriteLine();

        _logger.LogInformation(
            "Retrieved statistics for aggregate {AggregateId}: {EventCount} events, version {Version}",
            aggregateId,
            eventCount,
            version);

        return Result.Success();
    }
}