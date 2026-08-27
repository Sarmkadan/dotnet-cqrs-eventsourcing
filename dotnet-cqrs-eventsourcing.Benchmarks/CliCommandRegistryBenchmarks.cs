using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotNetCqrsEventSourcing.Infrastructure.Cli;
using DotNetCqrsEventSourcing.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Benchmarks;

[MemoryDiagnoser]
public class CliCommandRegistryBenchmarks
{
    private CliCommandRegistry _registry = null!;
    private List<MockCliCommand> _commands = null!;

    [Params(10, 100, 1000)]
    public int CommandCount;

    [GlobalSetup]
    public void Setup()
    {
        _commands = new List<MockCliCommand>();
        for (int i = 0; i < CommandCount; i++)
        {
            _commands.Add(new MockCliCommand($"command{i}", $"Description for command {i}"));
        }
        _registry = new CliCommandRegistry(_commands, NullLogger<CliCommandRegistry>.Instance);
    }

    [Benchmark]
    public bool TryResolve_ExistingCommand()
    {
        // Test resolving a command that exists (middle of the list)
        string[] args = { $"command{CommandCount / 2}", "arg1", "arg2" };
        return _registry.TryResolve(args, out _);
    }

    [Benchmark]
    public bool TryResolve_NonExistingCommand()
    {
        // Test resolving a command that doesn't exist
        string[] args = { "nonexistentcommand", "arg1", "arg2" };
        return _registry.TryResolve(args, out _);
    }

    [Benchmark]
    public bool TryResolve_EmptyArgs()
    {
        // Test resolving with empty args
        string[] args = { };
        return _registry.TryResolve(args, out _);
    }

    [Benchmark]
    public async System.Threading.Tasks.Task<Result> DispatchAsync_ExistingCommand()
    {
        // Test dispatching a command that exists
        string[] args = { $"command{CommandCount / 2}", "arg1", "arg2" };
        return await _registry.DispatchAsync(args);
    }

    [Benchmark]
    public async System.Threading.Tasks.Task<Result> DispatchAsync_NonExistingCommand()
    {
        // Test dispatching a command that doesn't exist
        string[] args = { "nonexistentcommand", "arg1", "arg2" };
        return await _registry.DispatchAsync(args);
    }

    [Benchmark]
    public void PrintHelp()
    {
        // Test printing help (outputs to console)
        _registry.PrintHelp();
    }

    private class MockCliCommand : ICliCommand
    {
        public MockCliCommand(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }

        public System.Threading.Tasks.Task<Result> ExecuteAsync(string[] args, System.Threading.CancellationToken cancellationToken = default)
        {
            // Simple successful execution
            return System.Threading.Tasks.Task.FromResult(Result.Success());
        }

        public void PrintUsage()
        {
            // Not used in benchmarks
        }
    }
}