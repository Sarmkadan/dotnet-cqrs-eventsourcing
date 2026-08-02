using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Benchmarks;

[MemoryDiagnoser]
public class CqrsHelpersBenchmarks
{
    private DummyCommand _command;

    [GlobalSetup]
    public void Setup()
    {
        _command = new DummyCommand { AggregateId = "123", Name = "TestCommand", Value = 1 };
        // Populate cache
        CqrsHelpers.GetHandlerMetadata(typeof(DummyCommand));
    }

    [Benchmark]
    public void GetHandlerMetadataBenchmark()
    {
        CqrsHelpers.GetHandlerMetadata(typeof(DummyCommand));
    }

    [Benchmark]
    public void ValidateCommandBenchmark()
    {
        CqrsHelpers.ValidateCommand(_command);
    }

    [Benchmark]
    public void ExtractAggregateIdBenchmark()
    {
        CqrsHelpers.ExtractAggregateId(_command);
    }

    public class DummyCommand
    {
        public string AggregateId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
