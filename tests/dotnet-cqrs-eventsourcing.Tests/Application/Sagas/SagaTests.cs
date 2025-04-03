#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application.Sagas;

using DotNetCqrsEventSourcing.Application.Sagas;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Domain.Sagas;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Xunit;

// ---------------------------------------------------------------------------
// Minimal saga implementation used only by these tests
// ---------------------------------------------------------------------------

public sealed class TestSaga : SagaBase
{
    public override string SagaName => "TestSaga";
    public int HandledEvents { get; private set; }

    public TestSaga() { }

    public TestSaga(string? correlationId) { SetCorrelation(correlationId); }

    public void SetCorrelation(string? id) { CorrelationId = id; }

    public void Handle(AccountCreatedEvent e)
    {
        Activate();
        HandledEvents++;
    }

    public void Finish()
    {
        Complete();
    }
}

public sealed class TestSagaHandler : ISagaHandler<TestSaga, AccountCreatedEvent>
{
    private readonly ISagaRepository<TestSaga> _repository;
    public TestSagaHandler(ISagaRepository<TestSaga> repository) => _repository = repository;

    public async Task<Result> HandleAsync(AccountCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var findResult = await _repository.FindByCorrelationIdAsync(@event.CorrelationId ?? @event.AggregateId, cancellationToken);
        TestSaga saga;
        if (findResult.IsSuccess)
        {
            saga = findResult.Data!;
        }
        else
        {
            saga = new TestSaga();
            saga.SetCorrelation(@event.CorrelationId ?? @event.AggregateId);
        }
        saga.Handle(@event);
        return await _repository.SaveAsync(saga, cancellationToken);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class SagaTests
{
    [Fact]
    public void SagaBase_InitialState_IsNotStarted()
    {
        var saga = new TestSaga();
        saga.State.Should().Be(SagaState.NotStarted);
        saga.OutboxEvents.Should().BeEmpty();
        saga.LastUpdatedAt.Should().BeNull();
    }

    [Fact]
    public void SagaBase_Activate_TransitionsToActive()
    {
        var saga = new TestSaga();
        saga.Handle(new AccountCreatedEvent("a-1", "ACC", "Test", "USD", 0m) { CorrelationId = "corr-1" });
        saga.State.Should().Be(SagaState.Active);
        saga.LastUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SagaBase_Complete_TransitionsToCompleted()
    {
        var saga = new TestSaga();
        saga.Handle(new AccountCreatedEvent("a-1", "ACC", "Test", "USD", 0m) { CorrelationId = "corr-2" });
        saga.Finish();
        saga.State.Should().Be(SagaState.Completed);
    }

    [Fact]
    public async Task InMemorySagaRepository_SaveAndRetrieve_RoundTrips()
    {
        var repo = new InMemorySagaRepository<TestSaga>();
        var saga = new TestSaga();
        saga.Handle(new AccountCreatedEvent("a-2", "ACC", "User", "USD", 100m) { CorrelationId = "corr-3" });

        await repo.SaveAsync(saga);

        var result = await repo.GetByIdAsync(saga.SagaId);
        result.IsSuccess.Should().BeTrue();
        result.Data!.SagaId.Should().Be(saga.SagaId);
        result.Data.State.Should().Be(SagaState.Active);
    }

    [Fact]
    public async Task InMemorySagaRepository_FindByCorrelation_ReturnsMatchingSaga()
    {
        var repo = new InMemorySagaRepository<TestSaga>();
        var saga = new TestSaga("my-corr");
        await repo.SaveAsync(saga);

        var result = await repo.FindByCorrelationIdAsync("my-corr");
        result.IsSuccess.Should().BeTrue();
        result.Data!.CorrelationId.Should().Be("my-corr");
    }

    [Fact]
    public async Task InMemorySagaRepository_GetAllFilteredByState_ReturnsCorrectSubset()
    {
        var repo = new InMemorySagaRepository<TestSaga>();

        var active = new TestSaga();
        active.Handle(new AccountCreatedEvent("a-3", "ACC", "U", "USD", 0m));
        await repo.SaveAsync(active);

        var notStarted = new TestSaga();
        await repo.SaveAsync(notStarted);

        var activeResult = await repo.GetAllAsync(SagaState.Active);
        activeResult.Data!.Should().HaveCount(1);

        var allResult = await repo.GetAllAsync();
        allResult.Data!.Should().HaveCount(2);
    }

    [Fact]
    public async Task SagaHandler_ProcessesEvent_PersistsSagaState()
    {
        var repo = new InMemorySagaRepository<TestSaga>();
        var handler = new TestSagaHandler(repo);
        var @event = new AccountCreatedEvent("a-4", "ACC-400", "Alice", "USD", 500m)
        {
            CorrelationId = "corr-handler-1"
        };

        var result = await handler.HandleAsync(@event);

        result.IsSuccess.Should().BeTrue();
        var find = await repo.FindByCorrelationIdAsync("corr-handler-1");
        find.IsSuccess.Should().BeTrue();
        find.Data!.HandledEvents.Should().Be(1);
        find.Data.State.Should().Be(SagaState.Active);
    }

    [Fact]
    public async Task SagaHandler_SecondEvent_AccumulatesOnExistingSaga()
    {
        var repo = new InMemorySagaRepository<TestSaga>();
        var handler = new TestSagaHandler(repo);

        var e1 = new AccountCreatedEvent("a-5", "ACC-5", "Bob", "USD", 200m) { CorrelationId = "corr-2nd" };
        var e2 = new AccountCreatedEvent("a-5", "ACC-5", "Bob", "USD", 200m) { CorrelationId = "corr-2nd" };

        await handler.HandleAsync(e1);
        await handler.HandleAsync(e2);

        var find = await repo.FindByCorrelationIdAsync("corr-2nd");
        find.Data!.HandledEvents.Should().Be(2);
    }
}
