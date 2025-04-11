# Performance Benchmarks for dotnet-cqrs-eventsourcing

This project contains performance benchmarks for the [dotnet-cqrs-eventsourcing](https://github.com/sarmkadan/dotnet-cqrs-eventsourcing) framework using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Overview

The benchmarks measure critical operations of the CQRS and Event Sourcing framework, including:

- Event store operations (append, retrieve)
- Aggregate root replay performance
- Account service lifecycle operations
- Memory allocations and garbage collection pressure

## Running Benchmarks

### Prerequisites

- .NET 10 SDK or later
- BenchmarkDotNet (included via NuGet)

### Running All Benchmarks

```bash
cd dotnet-cqrs-eventsourcing.Benchmarks
dotnet run -c Release
```

### Running Specific Benchmark

```bash
# Run a specific benchmark class
dotnet run -c Release -- --filter "*EventStoreBenchmarks*"

# Run a specific benchmark method
dotnet run -c Release -- --filter "*EventStoreBenchmarks*EventStore_AppendSingleEvent"
```

### Exporting Results

```bash
# Export to CSV
dotnet run -c Release -- --exporters csv --output ./results

# Export to HTML report
dotnet run -c Release -- --exporters html --output ./results

# Export to Markdown
dotnet run -c Release -- --exporters markdown --output ./results
```

### Viewing Detailed Statistics

```bash
# Show memory allocations
dotnet run -c Release -- --memory

# Show detailed statistics
dotnet run -c Release -- --statistics
```

## Benchmark Categories

### EventStore Operations
Measures the performance of event persistence and retrieval operations.

- **EventStore_AppendSingleEvent**: Throughput for appending individual events
- **EventStore_AppendBatchOf100Events**: Throughput for batch event appends
- **EventStore_GetEventsByAggregateId**: Performance of retrieving all events for an aggregate
- **EventStore_GetEventsFromVersion**: Performance of retrieving events from a specific version
- **EventStore_GetAggregateVersion**: Performance of version lookup for optimistic concurrency

### AggregateRoot Operations
Measures the performance of aggregate root reconstruction from event streams.

- **AggregateRoot_Replay100Events**: Replay 100 events
- **AggregateRoot_Replay1000Events**: Replay 1,000 events
- **AggregateRoot_Replay10000Events**: Replay 10,000 events

### AccountService Operations
Measures the performance of complete business workflows.

- **AccountService_CreateAccount**: Account creation with event persistence
- **AccountService_CompleteLifecycle**: Complete account lifecycle with 100 transactions

## Interpreting Results

BenchmarkDotNet provides several key metrics:

- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Median**: 50th percentile (P50)
- **P95**: 95th percentile (95% of operations are faster than this)
- **Allocated**: Memory allocated per operation (bytes)
- **Gen 0/1/2**: Garbage collection generations triggered

## Performance Targets

Based on the framework's design goals:

- **Event append**: < 1ms per event (in-memory)
- **Aggregate replay (100 events)**: < 1ms
- **Aggregate replay (1,000 events)**: < 10ms
- **Aggregate replay (10,000 events)**: < 100ms
- **Memory allocations**: Minimal allocations for hot paths

## Continuous Integration

Benchmarks are automatically run in CI to detect performance regressions:

```bash
# Run benchmarks in CI mode
dotnet run -c Release -- --join
```

## Adding New Benchmarks

To add new benchmarks:

1. Create a new benchmark class in the `Benchmarks/` directory
2. Decorate with `[MemoryDiagnoser]` for memory tracking
3. Use `[Benchmark]` attribute on public methods
4. Follow existing patterns for setup and cleanup
5. Add appropriate categories and documentation

Example:

```csharp
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class NewBenchmarkCategory
{
    private IServiceProvider _services = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCqrsFramework();
        _services = services.BuildServiceProvider();
    }

    [Benchmark]
    public void NewOperation()
    {
        // Your benchmark code here
    }
}
```

## Results Summary

The framework is designed for high performance with minimal overhead:

- Event sourcing operations are optimized for in-memory scenarios
- Aggregate replay uses efficient event application logic
- The `Result<T>` pattern avoids exception-based control flow
- Snapshots can reduce replay time by up to 90% for large aggregates

## Troubleshooting

### Benchmarks are too slow

- Ensure you're running in Release mode: `dotnet run -c Release`
- Close other applications to reduce system noise
- Run multiple times to warm up the runtime

### Memory allocations are high

- Check for unnecessary object allocations in hot paths
- Use `ArrayPool<T>` for temporary buffers
- Consider using structs for small, frequently-used types

### Benchmark results vary widely

- Run with `--iterationCount 15` for more stable results
- Use `--warmupCount 3` to warm up the runtime
- Ensure no other CPU-intensive processes are running

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [dotnet-cqrs-eventsourcing Main Repository](https://github.com/sarmkadan/dotnet-cqrs-eventsourcing)
- [Event Sourcing Pattern](https://martinfowler.com/eaaDev/EventSourcing.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

---

*Benchmarks last updated: 2026-07-01*
