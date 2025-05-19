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
/// CLI command for rebuilding read-model projections by replaying events from the
/// event store for one or more aggregates.
/// <para>
/// <b>Usage:</b>
/// <code>
///   dotnet run -- rebuild-read-models --aggregate &lt;id&gt;
///   dotnet run -- rebuild-read-models --all
/// </code>
/// </para>
/// <para>
/// Options:<br/>
/// <c>--aggregate &lt;id&gt;</c> – Rebuild projections for a single aggregate.<br/>
/// <c>--all</c>             – Rebuild all projections tracked by the projection service.<br/>
/// <c>--dry-run</c>         – Print what would be rebuilt without making changes.<br/>
/// </para>
/// </summary>
public sealed class ReadModelRebuilderCommand : ICliCommand
{
    private readonly IProjectionService _projectionService;
    private readonly IEventStore _eventStore;
    private readonly ILogger<ReadModelRebuilderCommand> _logger;

    public string Name => "rebuild-read-models";
    public string Description => "Rebuilds read-model projections by replaying events from the event store.";

    public ReadModelRebuilderCommand(
        IProjectionService projectionService,
        IEventStore eventStore,
        ILogger<ReadModelRebuilderCommand> logger)
    {
        _projectionService = GuardClauses.NotNull(projectionService, nameof(projectionService));
        _eventStore = GuardClauses.NotNull(eventStore, nameof(eventStore));
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var isDryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
        var allFlag = args.Contains("--all", StringComparer.OrdinalIgnoreCase);

        var aggregateIdIndex = Array.IndexOf(args, "--aggregate");
        if (aggregateIdIndex < 0)
            aggregateIdIndex = Array.IndexOf(args, "-a");

        string? aggregateId = null;
        if (aggregateIdIndex >= 0 && aggregateIdIndex + 1 < args.Length)
            aggregateId = args[aggregateIdIndex + 1];

        if (!allFlag && aggregateId is null)
        {
            PrintUsage();
            return Result.Failure("MISSING_ARGUMENT", "Specify --aggregate <id> or --all.");
        }

        if (isDryRun)
        {
            var target = allFlag ? "all aggregates" : $"aggregate '{aggregateId}'";
            Console.WriteLine($"[dry-run] Would rebuild read models for {target}.");
            _logger.LogInformation("[dry-run] Read model rebuild for {Target} – no changes made.", target);
            return Result.Success();
        }

        if (allFlag)
        {
            return await RebuildAllAsync(cancellationToken);
        }

        return await RebuildSingleAsync(aggregateId!, cancellationToken);
    }

    /// <inheritdoc/>
    public void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine($"Usage: dotnet run -- {Name} [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --aggregate <id>   Rebuild projections for a single aggregate.");
        Console.WriteLine("  --all              Rebuild projections for all tracked aggregates.");
        Console.WriteLine("  --dry-run          Print what would be rebuilt without applying changes.");
        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<Result> RebuildSingleAsync(string aggregateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rebuilding read models for aggregate {AggregateId}.", aggregateId);
        Console.WriteLine($"Rebuilding read models for aggregate '{aggregateId}'...");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _projectionService.RebuildProjectionAsync(aggregateId, cancellationToken);
        sw.Stop();

        if (result.IsSuccess)
        {
            Console.WriteLine($"  Done in {sw.ElapsedMilliseconds} ms.");
            _logger.LogInformation("Projection rebuild succeeded for {AggregateId} in {Elapsed} ms.", aggregateId, sw.ElapsedMilliseconds);
        }
        else
        {
            Console.Error.WriteLine($"  Failed: {result.ErrorMessage}");
            _logger.LogError("Projection rebuild failed for {AggregateId}: {Error}", aggregateId, result.ErrorMessage);
        }

        return result;
    }

    private async Task<Result> RebuildAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rebuilding all read model projections.");
        Console.WriteLine("Rebuilding all read model projections...");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _projectionService.RebuildAllProjectionsAsync(cancellationToken);
        sw.Stop();

        if (result.IsSuccess)
        {
            Console.WriteLine($"  Done in {sw.ElapsedMilliseconds} ms.");
            _logger.LogInformation("Full projection rebuild succeeded in {Elapsed} ms.", sw.ElapsedMilliseconds);
        }
        else
        {
            Console.Error.WriteLine($"  Failed: {result.ErrorMessage}");
            _logger.LogError("Full projection rebuild failed: {Error}", result.ErrorMessage);
        }

        return result;
    }
}
