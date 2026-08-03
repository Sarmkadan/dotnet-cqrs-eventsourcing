using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.ReadModels;

namespace DotNetCqrsEventSourcing.Benchmarks;

[MemoryDiagnoser]
public class InMemoryReadModelStoreBenchmarks
{
    private InMemoryReadModelStore<TestReadModel> _store = null!;
    private const string TestKey = "test-key";
    private readonly TestReadModel _testModel = new("test-data");

    [Params(10, 100, 1000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _store = new InMemoryReadModelStore<TestReadModel>();
        for (int i = 0; i < ItemCount; i++)
        {
            _store.UpsertAsync($"key-{i}", new TestReadModel($"data-{i}")).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public async Task UpsertAsync()
    {
        await _store.UpsertAsync(TestKey, _testModel);
    }

    [Benchmark]
    public async Task GetAsync()
    {
        await _store.GetAsync("key-0");
    }

    [Benchmark]
    public async Task GetAllAsync()
    {
        await _store.GetAllAsync();
    }

    [Benchmark]
    public async Task QueryAsync()
    {
        await _store.QueryAsync(m => m.Data.Contains("5"));
    }

    [Benchmark]
    public async Task GetCountAsync()
    {
        await _store.GetCountAsync();
    }

    public record TestReadModel(string Data);
}
