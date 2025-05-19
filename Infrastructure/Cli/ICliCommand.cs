#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Infrastructure.Cli;

using Shared.Results;

/// <summary>
/// Contract for a named CLI command that can be dispatched by the
/// <see cref="CliCommandRegistry"/>.
/// </summary>
public interface ICliCommand
{
    /// <summary>The command name as it appears on the command line (e.g. "rebuild-read-models").</summary>
    string Name { get; }

    /// <summary>Short description shown in the help listing.</summary>
    string Description { get; }

    /// <summary>
    /// Executes the command with the supplied argument tokens.
    /// Tokens are the raw arguments that follow the command name.
    /// </summary>
    Task<Result> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);

    /// <summary>Prints usage information for this command to standard output.</summary>
    void PrintUsage();
}
