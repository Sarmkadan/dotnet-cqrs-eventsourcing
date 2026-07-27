#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Cli;

using Microsoft.Extensions.Logging;
using ReadModels;
using Shared.Results;
using Utilities;

/// <summary>
/// CLI command for replaying dead-lettered events back through their projection runner.
/// <para>
/// <b>Usage:</b>
/// <code>
///   dotnet run -- dead-letter-replay --id &lt;entryId&gt;
///   dotnet run -- dead-letter-replay --all [--projection &lt;name&gt;]
///   dotnet run -- dead-letter-replay --list [--projection &lt;name&gt;]
/// </code>
/// </para>
/// <para>
/// Options:<br/>
/// <c>--id &lt;entryId&gt;</c>       – Replay a single dead-letter entry by id.<br/>
/// <c>--all</c>                     – Replay every eligible unresolved entry.<br/>
/// <c>--projection &lt;name&gt;</c> – Restrict <c>--all</c>/<c>--list</c> to one projection.<br/>
/// <c>--list</c>                    – List unresolved dead-letter entries without replaying.<br/>
/// </para>
/// </summary>
public sealed class DeadLetterReplayCommand : ICliCommand
{
    private readonly DeadLetterReplayService _replayService;
    private readonly IDeadLetterStore _deadLetterStore;
    private readonly ILogger<DeadLetterReplayCommand> _logger;

    /// <inheritdoc/>
    public string Name => "dead-letter-replay";

    /// <inheritdoc/>
    public string Description => "Replays dead-lettered events back to their projection, honoring per-stream ordering.";

    /// <summary>Initializes the command with the replay service, dead-letter store, and logger.</summary>
    /// <exception cref="ArgumentNullException">Any constructor argument is <see langword="null"/>.</exception>
    public DeadLetterReplayCommand(
        DeadLetterReplayService replayService,
        IDeadLetterStore deadLetterStore,
        ILogger<DeadLetterReplayCommand> logger)
    {
        _replayService = GuardClauses.NotNull(replayService, nameof(replayService));
        _deadLetterStore = GuardClauses.NotNull(deadLetterStore, nameof(deadLetterStore));
        _logger = GuardClauses.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/>.</exception>
    public async Task<Result> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return Result.Success();
        }

        var projectionName = GetOptionValue(args, "--projection");

        if (args.Contains("--list"))
            return await ListAsync(projectionName, cancellationToken);

        var entryId = GetOptionValue(args, "--id");
        if (entryId is not null)
            return await ReplayOneAsync(entryId, cancellationToken);

        if (args.Contains("--all"))
            return await ReplayAllAsync(projectionName, cancellationToken);

        PrintUsage();
        return Result.Failure("MISSING_ARGUMENT", "Specify --id <entryId>, --all, or --list.");
    }

    /// <inheritdoc/>
    public void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine($"Usage: dotnet run -- {Name} [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --id <entryId>        Replay a single dead-letter entry.");
        Console.WriteLine("  --all                 Replay every eligible unresolved entry.");
        Console.WriteLine("  --projection <name>   Restrict --all/--list to one projection.");
        Console.WriteLine("  --list                List unresolved dead-letter entries without replaying.");
        Console.WriteLine();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<Result> ListAsync(string? projectionName, CancellationToken cancellationToken)
    {
        var entries = string.IsNullOrWhiteSpace(projectionName)
            ? await _deadLetterStore.GetAllAsync(includeReprocessed: false, cancellationToken)
            : await _deadLetterStore.GetByProjectionAsync(projectionName, cancellationToken);

        Console.WriteLine($"Unresolved dead-letter entries: {entries.Count}");
        Console.WriteLine("=================================");

        foreach (var entry in entries)
        {
            Console.WriteLine(
                $"  id={entry.Id} projection={entry.ProjectionName} aggregate={entry.Event.AggregateId} " +
                $"version={entry.Event.AggregateVersion} attempts={entry.AttemptCount} error=\"{entry.ErrorMessage}\"");
        }

        Console.WriteLine();
        return Result.Success();
    }

    private async Task<Result> ReplayOneAsync(string entryId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Replaying dead-letter entry '{entryId}'...");

        var result = await _replayService.ReplayAsync(entryId, cancellationToken);

        if (result.IsSuccess)
        {
            Console.WriteLine("  Replayed and marked resolved.");
            _logger.LogInformation("Dead-letter entry {EntryId} replayed via CLI.", entryId);
        }
        else
        {
            Console.Error.WriteLine($"  Failed: {result.ErrorMessage}");
            _logger.LogError("Dead-letter replay failed for {EntryId}: {Error}", entryId, result.ErrorMessage);
        }

        return result;
    }

    private async Task<Result> ReplayAllAsync(string? projectionName, CancellationToken cancellationToken)
    {
        var target = string.IsNullOrWhiteSpace(projectionName) ? "all projections" : $"projection '{projectionName}'";
        Console.WriteLine($"Replaying dead-letter entries for {target}...");

        var result = await _replayService.ReplayAllAsync(projectionName, cancellationToken);
        if (!result.IsSuccess)
        {
            Console.Error.WriteLine($"  Failed: {result.ErrorMessage}");
            return Result.Failure(result.ErrorCode!, result.ErrorMessage!);
        }

        var batch = result.Data!;
        Console.WriteLine($"  Succeeded: {batch.Succeeded.Count}");
        Console.WriteLine($"  Failed: {batch.Failed.Count}");

        foreach (var (entryId, reason) in batch.Failed)
            Console.WriteLine($"    {entryId}: {reason}");

        _logger.LogInformation(
            "Dead-letter batch replay for {Target}: {Succeeded} succeeded, {Failed} failed.",
            target, batch.Succeeded.Count, batch.Failed.Count);

        return batch.Failed.Count == 0
            ? Result.Success()
            : Result.Failure("PARTIAL_REPLAY_FAILURE", $"{batch.Failed.Count} entr{(batch.Failed.Count == 1 ? "y" : "ies")} could not be replayed.");
    }

    private static string? GetOptionValue(string[] args, string optionName)
    {
        var index = Array.IndexOf(args, optionName);
        if (index < 0 || index + 1 >= args.Length)
            return null;

        var value = args[index + 1];
        return value.StartsWith('-') ? null : value;
    }
}
