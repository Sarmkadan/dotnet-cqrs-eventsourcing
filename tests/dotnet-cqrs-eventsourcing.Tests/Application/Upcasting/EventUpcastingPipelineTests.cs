#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application.Upcasting;

using System.Text.Json.Nodes;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Application.Upcasting;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.AggregateRoots;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Contains tests for the event upcasting pipeline, covering replay of streams that mix
/// legacy (schema version 1) and current (schema version 2) <c>AccountCreated</c> payloads.
/// </summary>
public sealed class EventUpcastingPipelineTests
{
    private const string LegacyAccountId = "ACC-LEGACY-1";
    private const string LegacyAccountCreatedJson =
        """{"AccountNumber":"ACC-LEGACY-1","AccountHolder":"Legacy Holder","Currency":"USD","InitialBalance":100,"EventId":"e1","AggregateId":"ACC-LEGACY-1","AggregateType":"Account","AggregateVersion":1,"OccurredAt":"2024-01-01T00:00:00Z"}""";
    private const string LegacyMoneyDepositedJson =
        """{"Amount":50,"Reference":"legacy-deposit","ProcessedAt":"2024-01-02T00:00:00Z","EventId":"e2","AggregateId":"ACC-LEGACY-1","AggregateType":"Account","AggregateVersion":2,"OccurredAt":"2024-01-02T00:00:00Z"}""";

    private readonly InMemoryEventRepository _eventRepository = new();
    private readonly EventTypeRegistry _registry = CreateRegistry();
    private readonly EventUpcastingPipeline _pipeline;

    /// <summary>
    /// Initializes the pipeline with the production <c>AccountCreated</c> V1-to-V2 upcaster.
    /// </summary>
    public EventUpcastingPipelineTests()
    {
        _pipeline = new EventUpcastingPipeline(_registry, new IEventUpcaster[] { new AccountCreatedV1ToV2Upcaster() });
    }

    /// <summary>
    /// Verifies that a stream written entirely under schema version 1 rehydrates into the current aggregate.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_LegacyStream_RehydratesAccountThroughUpcaster()
    {
        // Arrange
        await SaveLegacyAccountStreamAsync();
        var repository = new AccountRepository(_eventRepository, _pipeline);

        // Act
        var result = await repository.GetByIdAsync(LegacyAccountId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.AccountHolder.Should().Be("Legacy Holder");
        result.Data.Balance.CurrentAmount.Amount.Should().Be(150m);
    }

    /// <summary>
    /// Verifies that a store holding both a legacy account and an account written by the current code
    /// rehydrates both correctly.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_MixedVersionStore_RehydratesLegacyAndCurrentAccounts()
    {
        // Arrange
        await SaveLegacyAccountStreamAsync();
        var repository = new AccountRepository(_eventRepository, _pipeline);

        var current = new Account("ACC-CURRENT-1");
        current.CreateAccount("ACC-CURRENT-1", "Current Holder", "EUR", 200m);
        current.Deposit(25m, "current-deposit");
        (await repository.SaveAsync(current)).IsSuccess.Should().BeTrue();

        // Act
        var legacy = await repository.GetByIdAsync(LegacyAccountId);
        var modern = await repository.GetByIdAsync("ACC-CURRENT-1");

        // Assert
        legacy.Data!.AccountHolder.Should().Be("Legacy Holder");
        legacy.Data.Balance.CurrentAmount.Amount.Should().Be(150m);
        modern.Data!.AccountHolder.Should().Be("Current Holder");
        modern.Data.Balance.CurrentAmount.Amount.Should().Be(225m);
    }

    /// <summary>
    /// Verifies that the event store returns the upcast <c>AccountCreated</c> event from a legacy envelope.
    /// </summary>
    [Fact]
    public async Task GetEventStreamAsync_LegacyEnvelope_ReturnsUpcastEvent()
    {
        // Arrange
        await SaveLegacyAccountStreamAsync();
        var eventStore = new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry, _pipeline);

        // Act
        var result = await eventStore.GetEventStreamAsync(LegacyAccountId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        var created = result.Data[0].Should().BeOfType<AccountCreatedEvent>().Subject;
        created.AccountHolder.Should().Be("Legacy Holder");
        created.AccountNumber.Should().Be("ACC-LEGACY-1");
    }

    /// <summary>
    /// Verifies that newly appended events are stamped with the current schema version of their type,
    /// and that the rename is written to the payload.
    /// </summary>
    [Fact]
    public async Task AppendEventAsync_NewEvents_StampCurrentSchemaVersion()
    {
        // Arrange
        var eventStore = new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry, _pipeline);
        var created = new AccountCreatedEvent("ACC-NEW-1", "ACC-NEW-1", "New Holder", "USD", 10m);
        var deposited = new MoneyDepositedEvent("ACC-NEW-1", 5m, "ref", 2);

        // Act
        (await eventStore.AppendEventsAsync(new List<DomainEvent> { created, deposited })).IsSuccess.Should().BeTrue();
        var stored = (await _eventRepository.GetEventsByAggregateIdAsync("ACC-NEW-1")).Data!;

        // Assert
        var createdEnvelope = stored.Single(e => e.EventType == "AccountCreated");
        createdEnvelope.SchemaVersion.Should().Be(2);
        JsonNode.Parse(createdEnvelope.EventData)!["OwnerName"]!.GetValue<string>().Should().Be("New Holder");
        stored.Single(e => e.EventType == "MoneyDeposited").SchemaVersion.Should().Be(1);
    }

    /// <summary>
    /// Verifies that an envelope already at the current version is returned without modification.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_CurrentVersionEnvelope_ReturnsPayloadUnchanged()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "AccountCreated",
            EventData = """{"OwnerName":"Modern"}""",
            SchemaVersion = 2
        };

        // Act
        var payload = _pipeline.UpcastToCurrentVersion(envelope);

        // Assert
        payload.Should().Be("""{"OwnerName":"Modern"}""");
    }

    /// <summary>
    /// Verifies that a version 1 payload already carrying the version 2 key is left intact.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_LegacyEnvelopeWithRenamedKey_IsNotRenamedTwice()
    {
        // Arrange
        var envelope = new EventEnvelope
        {
            EventType = "AccountCreated",
            EventData = """{"OwnerName":"Already Renamed"}""",
            SchemaVersion = 1
        };

        // Act
        var payload = JsonNode.Parse(_pipeline.UpcastToCurrentVersion(envelope))!.AsObject();

        // Assert
        payload.Should().HaveCount(1);
        payload["OwnerName"]!.GetValue<string>().Should().Be("Already Renamed");
    }

    /// <summary>
    /// Verifies that several upcasters are applied in order to reach the version declared by the event type.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_MultiStepChain_AppliesEveryStepInOrder()
    {
        // Arrange
        var registry = CreateRegistry();
        registry.Register<UpcastTestEvent>(UpcastTestEvent.Name);
        var pipeline = new EventUpcastingPipeline(registry, new IEventUpcaster[]
        {
            new RenameUpcaster(UpcastTestEvent.Name, 1, "Old", "Mid"),
            new RenameUpcaster(UpcastTestEvent.Name, 2, "Mid", "New")
        });
        var envelope = new EventEnvelope
        {
            EventType = UpcastTestEvent.Name,
            EventData = """{"Old":"value"}""",
            SchemaVersion = 1
        };

        // Act
        var payload = JsonNode.Parse(pipeline.UpcastToCurrentVersion(envelope))!.AsObject();

        // Assert
        payload.Should().ContainKey("New");
        payload.Should().NotContainKey("Old");
        payload.Should().NotContainKey("Mid");
        payload["New"]!.GetValue<string>().Should().Be("value");
    }

    /// <summary>
    /// Verifies that a legacy envelope with no registered upcaster fails loudly instead of deserializing silently.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_MissingUpcaster_ThrowsInvalidOperationException()
    {
        // Arrange
        var pipeline = new EventUpcastingPipeline(_registry, Array.Empty<IEventUpcaster>());
        var envelope = new EventEnvelope { EventType = "AccountCreated", EventData = LegacyAccountCreatedJson, SchemaVersion = 1 };

        // Act
        var act = () => pipeline.UpcastToCurrentVersion(envelope);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccountCreated*from version 1*");
    }

    /// <summary>
    /// Verifies that an envelope written by a newer schema than this build understands is rejected.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_NewerSchemaVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var envelope = new EventEnvelope { EventType = "AccountCreated", EventData = "{}", SchemaVersion = 3 };

        // Act
        var act = () => _pipeline.UpcastToCurrentVersion(envelope);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*schema version 3*");
    }

    /// <summary>
    /// Verifies that an envelope with an unregistered event type is rejected by the registry.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_UnknownEventType_ThrowsUnknownEventTypeException()
    {
        // Arrange
        var envelope = new EventEnvelope { EventType = "NoSuchEvent", EventData = "{}", SchemaVersion = 1 };

        // Act
        var act = () => _pipeline.UpcastToCurrentVersion(envelope);

        // Assert
        act.Should().Throw<UnknownEventTypeException>();
    }

    /// <summary>
    /// Verifies that a null envelope is rejected.
    /// </summary>
    [Fact]
    public void UpcastToCurrentVersion_NullEnvelope_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _pipeline.UpcastToCurrentVersion(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that registering two upcasters for the same event type and version is rejected.
    /// </summary>
    [Fact]
    public void Constructor_DuplicateUpcasterForSameVersion_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => new EventUpcastingPipeline(_registry, new IEventUpcaster[]
        {
            new AccountCreatedV1ToV2Upcaster(),
            new AccountCreatedV1ToV2Upcaster()
        });

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccountCreated*version 1*");
    }

    /// <summary>
    /// Verifies that a null registry is rejected by the pipeline constructor.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EventUpcastingPipeline(null!, Array.Empty<IEventUpcaster>());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that the event version attribute defaults to version 1 and reads declared versions.
    /// </summary>
    [Fact]
    public void EventVersionAttribute_GetVersion_ReturnsDeclaredOrDefaultVersion()
    {
        // Assert
        EventVersionAttribute.GetVersion(typeof(MoneyDepositedEvent)).Should().Be(1);
        EventVersionAttribute.GetVersion(typeof(AccountCreatedEvent)).Should().Be(2);
    }

    private async Task SaveLegacyAccountStreamAsync()
    {
        var envelopes = new List<EventEnvelope>
        {
            new()
            {
                AggregateId = LegacyAccountId,
                AggregateType = "Account",
                AggregateVersion = 1,
                EventType = "AccountCreated",
                EventData = LegacyAccountCreatedJson,
                SchemaVersion = 1
            },
            new()
            {
                AggregateId = LegacyAccountId,
                AggregateType = "Account",
                AggregateVersion = 2,
                EventType = "MoneyDeposited",
                EventData = LegacyMoneyDepositedJson,
                SchemaVersion = 1
            }
        };

        (await _eventRepository.SaveEventsAsync(envelopes)).IsSuccess.Should().BeTrue();
    }

    private static EventTypeRegistry CreateRegistry()
    {
        var registry = new EventTypeRegistry();
        registry.ScanAssembly(typeof(DomainEvent).Assembly);
        return registry;
    }

    /// <summary>
    /// Test-only event with schema version 3, used to exercise a multi-step upcaster chain.
    /// </summary>
    [EventVersion(3)]
    public sealed class UpcastTestEvent : DomainEvent
    {
        /// <summary>The stable event name used for registration.</summary>
        public const string Name = "UpcastTest";

        /// <summary>Gets or sets the test payload value.</summary>
        public string? New { get; set; }

        /// <summary>Initializes a new, empty instance for deserialization.</summary>
        public UpcastTestEvent() { }

        /// <inheritdoc/>
        public override string GetEventType() => Name;
    }

    private sealed class RenameUpcaster : IEventUpcaster
    {
        private readonly string _from;
        private readonly string _to;

        public RenameUpcaster(string eventType, int fromVersion, string from, string to)
        {
            EventType = eventType;
            FromVersion = fromVersion;
            _from = from;
            _to = to;
        }

        public string EventType { get; }

        public int FromVersion { get; }

        public JsonObject Upcast(JsonObject payload)
        {
            payload.Remove(_from, out var value);
            payload[_to] = value;
            return payload;
        }
    }
}
