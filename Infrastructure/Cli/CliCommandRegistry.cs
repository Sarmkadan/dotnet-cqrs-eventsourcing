#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Cli;

using Microsoft.Extensions.Logging;
using Shared.Results;
using Utilities;

/// <summary>
/// Discovers and dispatches CLI commands by matching the first argument token
/// against all registered <see cref="ICliCommand"/> implementations.
/// <para>
/// Usage: <c>dotnet run -- &lt;command&gt; [options]</c><br/>
/// Example: <c>dotnet run -- rebuild-read-models --all</c>
/// </para>
/// </summary>
public sealed class CliCommandRegistry
{
    private readonly IReadOnlyDictionary<string, ICliCommand> _commands;
    private readonly ILogger<CliCommandRegistry> _logger;

    public CliCommandRegistry(IEnumerable<ICliCommand> commands, ILogger<CliCommandRegistry> logger)
    {
        _logger = GuardClauses.NotNull(logger, nameof(logger));
        _commands = GuardClauses.NotNull(commands, nameof(commands))
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("CliCommandRegistry initialized with {CommandCount} commands", _commands.Count);
    }

    /// <summary>
    /// Returns <see langword="true"/> and the matching command when <paramref name="args"/>
    /// starts with a known command name.
    /// </summary>
    public bool TryResolve(string[] args, out ICliCommand? command)
    {
        _logger.LogInformation("Trying to resolve CLI command from args {Args}", args);
        command = null;
        if (args.Length == 0)
        {
            _logger.LogInformation("No args provided for command resolution");
            return false;
        }
        var result = _commands.TryGetValue(args[0], out command);
        if (result)
        {
            _logger.LogInformation("Resolved CLI command {CommandName} from args {Args}", command?.Name, args);
        }
        else
        {
            _logger.LogInformation("No CLI command found for args {Args}", args);
        }
        return result;
    }

    /// <summary>
    /// Dispatches the command identified by <c>args[0]</c>, passing the remaining
    /// tokens as arguments to <see cref="ICliCommand.ExecuteAsync"/>.
    /// Returns a failure result when the command is not found.
    /// </summary>
    public async Task<Result> DispatchAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching CLI command with args {Args}", args);
        if (!TryResolve(args, out var command) || command is null)
        {
            PrintHelp();
            _logger.LogWarning("Unknown command '{Command}'", args.Length > 0 ? args[0] : "(none)");
            return Result.Failure("UNKNOWN_COMMAND", $"Unknown command '{(args.Length > 0 ? args[0] : "(none)")}'.");
        }

        _logger.LogInformation("Executing CLI command '{Command}'.", command.Name);

        var commandArgs = args.Skip(1).ToArray();
        var result = await command.ExecuteAsync(commandArgs, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("CLI command '{Command}' executed successfully.", command.Name);
        }
        else
        {
            _logger.LogError("CLI command '{Command}' failed with error: {Error}", command.Name, result.Error);
        }

        return result;
    }

    /// <summary>Prints a list of all registered commands to standard output.</summary>
    public void PrintHelp()
    {
        _logger.LogInformation("Printing help for CLI commands");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
            Console.WriteLine($"  {cmd.Name,-30} {cmd.Description}");
        Console.WriteLine();
        _logger.LogInformation("Help printed for CLI commands");
    }
}