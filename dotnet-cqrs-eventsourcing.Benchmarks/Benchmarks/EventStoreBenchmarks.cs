using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_cqrs_eventsourcing.Benchmarks.Benchmarks;

/// <summary>
/// Performance benchmarks for the Event Store and Aggregate Root operations.
/// Measures throughput, latency, and memory allocations for critical operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class EventStoreBenchmarks
{
    private IEventStore _eventStore = null!;
    private IEventRepository _eventRepository = null!;
    private ServiceProvider _serviceProvider = null!;
    private string _testAggregateId = "benchmark-aggregate-001";
    private List<DomainEvent> _generatedEvents = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddCqrsFramework();
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _eventStore = _serviceProvider.GetRequiredService<IEventStore>();
        _eventRepository = _serviceProvider.GetRequiredService<IEventRepository>();

        // Generate test events for replay benchmarks
        GenerateTestEvents(1000);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider?.Dispose();
    }

    private void GenerateTestEvents(int count)
    {
        _generatedEvents.Clear();

        for (int i = 0; i < count; i++)
        {
            var @event = new AccountCreatedEvent(
                _testAggregateId,
                $"ACC-{i:D6}",
                $"Account Holder {i}",
                "USD",
                1000m + i,
                DateTime.UtcNow.AddMinutes(-count + i)
            );
            _generatedEvents.Add(@event);
        }
    }

    /// <summary>
    /// Benchmark: Event append throughput (single event)
    /// Measures the performance of appending individual events to the event store.
    /// This is a critical operation for write-heavy CQRS systems.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("EventStore")]
    public async Task EventStore_AppendSingleEvent()
    {
        var @event = new AccountCreatedEvent(
            $"benchmark-aggregate-{Guid.NewGuid()}",
            "ACC-SINGLE",
            "Single Event Account",
            "USD",
            1000m,
            DateTime.UtcNow
        );

        await _eventStore.AppendEventAsync(@event);
    }

    /// <summary>
    /// Benchmark: Event append throughput (batch of 100 events)
    /// Measures the performance of appending events in batches, which is common
    /// in event sourcing when multiple events are raised during a single command.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("EventStore")]
    public async Task EventStore_AppendBatchOf100Events()
    {
        var events = new List<DomainEvent>();
        var aggregateId = $"benchmark-aggregate-batch-{Guid.NewGuid()}";

        for (int i = 0; i < 100; i++)
        {
            events.Add(new MoneyDepositedEvent(
                aggregateId,
                100m + i,
                $"DEP-{i:D3}",
                i + 1
            ));
        }

        await _eventStore.AppendEventsAsync(events);
    }

    /// <summary>
    /// Benchmark: Aggregate replay performance (100 events)
    /// Measures the time to replay 100 events to reconstruct an aggregate state.
    /// This is critical for read operations and query performance.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("AggregateRoot")]
    public async Task AggregateRoot_Replay100Events()
    {
        var aggregate = new Account(_testAggregateId);
        var events = _generatedEvents.Take(100).ToList();

        aggregate.ReplayEvents(events);
    }

    /// <summary>
    /// Benchmark: Aggregate replay performance (1,000 events)
    /// Measures the time to replay 1,000 events to reconstruct an aggregate state.
    /// Tests scalability with larger event streams.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("AggregateRoot")]
    public async Task AggregateRoot_Replay1000Events()
    {
        var aggregate = new Account(_testAggregateId);
        var events = _generatedEvents.Take(1000).ToList();

        aggregate.ReplayEvents(events);
    }

    /// <summary>
    /// Benchmark: Aggregate replay performance (10,000 events)
    /// Measures the time to replay 10,000 events to reconstruct an aggregate state.
    /// Tests extreme scalability scenarios.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("AggregateRoot")]
    public async Task AggregateRoot_Replay10000Events()
    {
        var aggregate = new Account(_testAggregateId);
        var events = _generatedEvents.Take(10000).ToList();

        aggregate.ReplayEvents(events);
    }

    /// <summary>
    /// Benchmark: Event retrieval by aggregate ID
    /// Measures the performance of retrieving all events for a specific aggregate.
    /// This is a common operation for read models and projections.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("EventStore")]
    public async Task EventStore_GetEventsByAggregateId()
    {
        await _eventStore.GetEventStreamAsync(_testAggregateId);
    }

    /// <summary>
    /// Benchmark: Event retrieval from specific version
    /// Measures the performance of retrieving events starting from a specific version.
    /// This is used when loading aggregates from snapshots.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("EventStore")]
    public async Task EventStore_GetEventsFromVersion()
    {
        await _eventStore.GetEventStreamFromVersionAsync(_testAggregateId, 500);
    }

    /// <summary>
    /// Benchmark: Aggregate version lookup
    /// Measures the performance of getting the current version of an aggregate.
    /// This is a lightweight operation used for optimistic concurrency checks.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("EventStore")]
    public async Task EventStore_GetAggregateVersion()
    {
        await _eventRepository.GetAggregateVersionAsync(_testAggregateId);
    }

    /// <summary>
    /// Benchmark: Account aggregate lifecycle (create + 100 transactions)
    /// Measures the complete lifecycle of creating an account and performing multiple transactions.
    /// This simulates a realistic business scenario.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("AccountService")]
    public async Task AccountService_CompleteLifecycle()
    {
        var accountService = _serviceProvider.GetRequiredService<IAccountService>();
        var aggregateId = $"lifecycle-{Guid.NewGuid()}";

        // Create account
        await accountService.CreateAccountAsync(aggregateId, "Benchmark User", "USD", 1000m);

        // Perform 100 transactions
        for (int i = 0; i < 100; i++)
        {
            await accountService.DepositAsync(aggregateId, 100m, $"DEP-{i:D3}");
            await accountService.WithdrawAsync(aggregateId, 50m, $"WTH-{i:D3}");
        }

        // Get final state
        await accountService.GetAccountAsync(aggregateId);
    }

    /// <summary>
    /// Benchmark: Account creation with event persistence
    /// Measures the complete flow from command to event persistence.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("AccountService")]
    public async Task AccountService_CreateAccount()
    {
        var accountService = _serviceProvider.GetRequiredService<IAccountService>();
        await accountService.CreateAccountAsync(
            $"create-{Guid.NewGuid()}",
            "Benchmark Account",
            "USD",
            5000m
        );
    }
}
