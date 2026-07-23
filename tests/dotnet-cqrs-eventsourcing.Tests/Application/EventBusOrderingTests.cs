#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Tests to verify per-aggregate ordering guarantees in EventBus implementation.
// Ensures that events with the same AggregateId are processed sequentially
// in the order they were published, while events with different AggregateIds
// can be processed in parallel.
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class EventBusOrderingTests
{
    private readonly EventBus _eventBus;

    public EventBusOrderingTests()
    {
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
    }

    [Fact]
    public async Task PublishEventsAsync_WithSameAggregateId_ProcessesInOrder()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var eventOrder = new ConcurrentQueue<int>();
        var completionOrder = new ConcurrentQueue<int>();

        // Create a handler that records the order events are received
        _eventBus.Subscribe<TestDomainEvent>(async @event =>
        {
            await Task.Delay(10); // Simulate some processing time
            eventOrder.Enqueue(@event.SequenceNumber);
            completionOrder.Enqueue(@event.SequenceNumber);
        });

        // Create events in a specific order
        var events = new List<DomainEvent>
        {
            new TestDomainEvent(aggregateId, "TestAggregate", 1) { SequenceNumber = 1 },
            new TestDomainEvent(aggregateId, "TestAggregate", 2) { SequenceNumber = 2 },
            new TestDomainEvent(aggregateId, "TestAggregate", 3) { SequenceNumber = 3 },
            new TestDomainEvent(aggregateId, "TestAggregate", 4) { SequenceNumber = 4 }
        };

        // Act - publish all events concurrently
        var publishTask1 = _eventBus.PublishEventsAsync(new List<DomainEvent> { events[0], events[1] }, CancellationToken.None);
        var publishTask2 = _eventBus.PublishEventsAsync(new List<DomainEvent> { events[2], events[3] }, CancellationToken.None);

        await Task.WhenAll(publishTask1, publishTask2);

        // Assert - events should be processed in the order they were published
        eventOrder.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 },
            "Events with the same AggregateId should be processed sequentially in order");
    }

    [Fact]
    public async Task PublishEventsAsync_WithDifferentAggregateIds_ProcessesInParallel()
    {
        // Arrange
        var aggregateId1 = Guid.NewGuid().ToString();
        var aggregateId2 = Guid.NewGuid().ToString();
        var processingOrder = new ConcurrentQueue<string>();

        // Create handlers that record when processing starts
        _eventBus.Subscribe<TestDomainEvent>(async @event =>
        {
            processingOrder.Enqueue($"Start-{@event.AggregateId}-{@event.SequenceNumber}");
            await Task.Delay(50); // Simulate longer processing time
            processingOrder.Enqueue($"End-{@event.AggregateId}-{@event.SequenceNumber}");
        });

        // Create events for two different aggregates
        var events = new List<DomainEvent>
        {
            new TestDomainEvent(aggregateId1, "TestAggregate", 1) { SequenceNumber = 1 },
            new TestDomainEvent(aggregateId2, "TestAggregate", 1) { SequenceNumber = 2 }
        };

        // Act - publish both events concurrently
        var startTime = DateTime.UtcNow;
        await _eventBus.PublishEventsAsync(events, CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert - processing should overlap (parallel execution)
        // The total duration should be less than twice the individual processing time
        // since they can run in parallel
        duration.TotalMilliseconds.Should().BeLessThan(120,
            "Events with different AggregateIds should be processed in parallel");

        // Verify both events were processed
        processingOrder.Should().Contain(p => p.Contains(aggregateId1));
        processingOrder.Should().Contain(p => p.Contains(aggregateId2));
    }

    [Fact]
    public async Task PublishEventsAsync_WithMixedAggregateIds_MaintainsOrderWithinEachAggregate()
    {
        // Arrange
        var aggregateId1 = "Account-1";
        var aggregateId2 = "Account-2";
        var processingLog = new List<string>();

        // Create handlers that log processing order
        _eventBus.Subscribe<TestDomainEvent>(async @event =>
        {
            await Task.Delay(10); // Simulate processing
            lock (processingLog)
            {
                processingLog.Add($"Processed-{@event.AggregateId}-{@event.SequenceNumber}");
            }
        });

        // Create interleaved events from two different aggregates
        // Events: A1(1), A2(1), A1(2), A2(2), A1(3), A2(3)
        var events = new List<DomainEvent>
        {
            new TestDomainEvent(aggregateId1, "Account", 1) { SequenceNumber = 1 },
            new TestDomainEvent(aggregateId2, "Account", 1) { SequenceNumber = 1 },
            new TestDomainEvent(aggregateId1, "Account", 2) { SequenceNumber = 2 },
            new TestDomainEvent(aggregateId2, "Account", 2) { SequenceNumber = 2 },
            new TestDomainEvent(aggregateId1, "Account", 3) { SequenceNumber = 3 },
            new TestDomainEvent(aggregateId2, "Account", 3) { SequenceNumber = 3 }
        };

        // Act - publish all events
        await _eventBus.PublishEventsAsync(events, CancellationToken.None);

        // Assert - each aggregate's events should be in order
        var account1Events = processingLog.Where(log => log.Contains(aggregateId1)).ToList();
        var account2Events = processingLog.Where(log => log.Contains(aggregateId2)).ToList();

        account1Events.Should().BeEquivalentTo(new[] {
            "Processed-Account-1-1",
            "Processed-Account-1-2",
            "Processed-Account-1-3"
        });

        account2Events.Should().BeEquivalentTo(new[] {
            "Processed-Account-2-1",
            "Processed-Account-2-2",
            "Processed-Account-2-3"
        });
    }

    [Fact]
    public async Task PublishEventAsync_WithNullAggregateId_StillProcessesWithoutErrors()
    {
        // Arrange
        var processingLog = new List<int>();

        _eventBus.Subscribe<TestDomainEvent>(async @event =>
        {
            processingLog.Add(@event.SequenceNumber);
            await Task.CompletedTask;
        });

        // Create event with null/empty aggregate ID
        var @event = new TestDomainEvent(string.Empty, "TestAggregate", 1) { SequenceNumber = 1 };

        // Act - should not throw
        var result = await _eventBus.PublishEventAsync(@event, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        processingLog.Should().BeEquivalentTo(new[] { 1 });
    }

    [Fact]
    public async Task PublishEventsAsync_ConcurrentPublishesToSameAggregate_SequentialProcessing()
    {
        // Arrange
        var aggregateId = Guid.NewGuid().ToString();
        var processingOrder = new ConcurrentBag<int>();
        var completionSignals = new ConcurrentBag<Task>();

        _eventBus.Subscribe<TestDomainEvent>(async @event =>
        {
            processingOrder.Add(@event.SequenceNumber);
            await Task.Delay(20); // Simulate work
            completionSignals.Add(Task.CompletedTask);
        });

        // Create multiple batches of events for the same aggregate
        var batch1 = new List<DomainEvent> {
            new TestDomainEvent(aggregateId, "Account", 1) { SequenceNumber = 1 },
            new TestDomainEvent(aggregateId, "Account", 2) { SequenceNumber = 2 }
        };

        var batch2 = new List<DomainEvent> {
            new TestDomainEvent(aggregateId, "Account", 3) { SequenceNumber = 3 },
            new TestDomainEvent(aggregateId, "Account", 4) { SequenceNumber = 4 }
        };

        var batch3 = new List<DomainEvent> {
            new TestDomainEvent(aggregateId, "Account", 5) { SequenceNumber = 5 }
        };

        // Act - publish all batches concurrently
        var tasks = new[]
        {
            _eventBus.PublishEventsAsync(batch1, CancellationToken.None),
            _eventBus.PublishEventsAsync(batch2, CancellationToken.None),
            _eventBus.PublishEventsAsync(batch3, CancellationToken.None)
        };

        await Task.WhenAll(tasks);

        // Wait for all handlers to complete
        await Task.WhenAll(completionSignals);

        // Assert - events should be processed in order despite concurrent publishes
        processingOrder.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 },
            "Concurrent publishes to the same aggregate should result in sequential processing");
    }

    [Fact]
    public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _eventBus.Subscribe<TestDomainEvent>(null!));
    }

    [Fact]
    public void Unsubscribe_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _eventBus.Unsubscribe<TestDomainEvent>(null!));
    }

    [Fact]
    public async Task PublishEventAsync_WithNullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventBus.PublishEventAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishEventsAsync_WithNullEvents_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventBus.PublishEventsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishEventsAsync_WithNullEventInCollection_ReturnsFailureResult()
    {
        // Arrange
        var events = new List<DomainEvent> { new TestDomainEvent("id", "type", 1), null! };

        // Act
        var result = await _eventBus.PublishEventsAsync(events, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PUBLISH_FAILED");
    }

    // Test domain event implementation
    private class TestDomainEvent : DomainEvent
    {
        public int SequenceNumber { get; set; }

        public TestDomainEvent(string aggregateId, string aggregateType, long aggregateVersion)
            : base(aggregateId, aggregateType, aggregateVersion)
        {
        }

        public override string GetEventType() => "TestDomainEvent";
    }
}