#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetCqrsEventSourcing.Tests.Application;

/// <summary>
/// Contains tests for event ingestion hardening: forged event type strings must never resolve to CLR types
/// and payloads above the configured size limit must be rejected before they are parsed or deserialized.
/// </summary>
public sealed class EventStoreIngestionTests
{
    private const string AggregateId = "ACC-INGEST-1";
    private const string AccountCreatedJson =
        """{"AccountNumber":"ACC-INGEST-1","AccountHolder":"Holder","Currency":"USD","InitialBalance":10,"EventId":"e1","AggregateId":"ACC-INGEST-1","AggregateType":"Account","AggregateVersion":1,"OccurredAt":"2024-01-01T00:00:00Z"}""";

    private readonly InMemoryEventRepository _eventRepository = new();
    private readonly EventTypeRegistry _registry = CreateRegistry();

    /// <summary>
    /// Verifies that the CLR assembly-qualified name of a registered event type is not accepted as an event name.
    /// </summary>
    [Fact]
    public void Resolve_AssemblyQualifiedTypeName_ThrowsUnknownEventTypeException()
    {
        // Arrange
        var forged = typeof(AccountCreatedEvent).AssemblyQualifiedName!;

        // Act
        var act = () => _registry.Resolve(forged);

        // Assert
        act.Should().Throw<UnknownEventTypeException>()
            .Which.EventTypeName.Should().Be(forged);
    }

    /// <summary>
    /// Verifies that a forged type string naming an arbitrary CLR type outside the event allow-list is rejected
    /// without reflection-based resolution.
    /// </summary>
    [Fact]
    public void TryResolve_ForgedClrTypeName_ReturnsFalseAndNullType()
    {
        // Arrange
        const string forged = "System.Diagnostics.Process, System.Diagnostics.Process";

        // Act
        var resolved = _registry.TryResolve(forged, out var type);

        // Assert
        resolved.Should().BeFalse();
        type.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a stored envelope carrying a valid payload under a forged type string is not deserialized into
    /// any event, so the forged name cannot select a CLR type.
    /// </summary>
    [Fact]
    public async Task GetEventStreamAsync_ForgedEventType_DoesNotDeserializeEvent()
    {
        // Arrange
        var forged = typeof(AccountCreatedEvent).AssemblyQualifiedName!;
        await SaveEnvelopeAsync(forged, AccountCreatedJson);
        var eventStore = new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry);

        // Act
        var result = await eventStore.GetEventStreamAsync(AggregateId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a stored payload above the limit fails the read instead of being parsed or silently dropped.
    /// </summary>
    [Fact]
    public async Task GetEventStreamAsync_OversizedPayload_FailsWithoutDeserializing()
    {
        // Arrange
        await SaveEnvelopeAsync("AccountCreated", new string('x', 4096));
        var eventStore = new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry, maxEventPayloadBytes: 1024);

        // Act
        var result = await eventStore.GetEventStreamAsync(AggregateId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("RETRIEVE_FAILED");
    }

    /// <summary>
    /// Verifies that appending an event whose serialized payload exceeds the limit is rejected and nothing is persisted.
    /// </summary>
    [Fact]
    public async Task AppendEventAsync_OversizedPayload_RejectsAndPersistsNothing()
    {
        // Arrange
        var eventStore = new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry, maxEventPayloadBytes: 256);
        var created = new AccountCreatedEvent(AggregateId, AggregateId, new string('h', 1024), "USD", 10m);

        // Act
        var result = await eventStore.AppendEventAsync(created);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("APPEND_FAILED");
        (await _eventRepository.GetEventsByAggregateIdAsync(AggregateId)).Data!.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a non-positive payload limit is rejected when the store is constructed.
    /// </summary>
    [Fact]
    public void Constructor_NonPositivePayloadLimit_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => new EventStore(_eventRepository, NullLogger<EventStore>.Instance, _registry, maxEventPayloadBytes: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxEventPayloadBytes");
    }

    private async Task SaveEnvelopeAsync(string eventType, string eventData)
    {
        var envelope = new EventEnvelope
        {
            AggregateId = AggregateId,
            AggregateType = "Account",
            AggregateVersion = 1,
            EventType = eventType,
            EventData = eventData,
            SchemaVersion = 1
        };

        (await _eventRepository.SaveEventsAsync(new List<EventEnvelope> { envelope })).IsSuccess.Should().BeTrue();
    }

    private static EventTypeRegistry CreateRegistry()
    {
        var registry = new EventTypeRegistry();
        registry.ScanAssembly(typeof(DomainEvent).Assembly);
        return registry;
    }
}
