#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Application;

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public sealed class EventStoreCompactionServiceTests
{
    private readonly InMemoryEventRepository _repository;
    private readonly Mock<ISnapshotService> _snapshotMock;
    private readonly EventStoreCompactionService _sut;

    public EventStoreCompactionServiceTests()
    {
        _repository = new InMemoryEventRepository();
        _snapshotMock = new Mock<ISnapshotService>();
        _sut = new EventStoreCompactionService(
            _repository,
            _snapshotMock.Object,
            NullLogger<EventStoreCompactionService>.Instance);
    }

    private static EventEnvelope MakeEnvelope(string aggregateId, long version) =>
        new EventEnvelope
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = aggregateId,
            AggregateType = "Account",
            AggregateVersion = version,
            EventType = "AccountCreated",
            EventData = "{}",
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task CompactToVersionAsync_RemovesEventsBeforeVersion()
    {
        const string id = "agg-1";
        // Seed 5 events at versions 1-5
        for (long v = 1; v <= 5; v++)
        {
            var env = MakeEnvelope(id, v);
            await _repository.SaveEventsAsync(new List<EventEnvelope> { env });
        }

        // Compact – keep from version 4 (delete 1,2,3)
        var result = await _sut.CompactToVersionAsync(id, keepFromVersion: 4);

        result.IsSuccess.Should().BeTrue();
        result.Data!.EventsRemoved.Should().Be(3);
        result.Data.CompactedToVersion.Should().Be(4);
        result.Data.AggregateId.Should().Be(id);

        // Only versions 4 and 5 should remain
        var remaining = await _repository.GetEventsByAggregateIdAsync(id);
        remaining.Data!.Should().HaveCount(2);
        remaining.Data.Select(e => e.AggregateVersion).Should().BeEquivalentTo(new long[] { 4, 5 });
    }

    [Fact]
    public async Task CompactAsync_WithSnapshot_UsesSnapshotVersion()
    {
        const string id = "agg-2";
        for (long v = 1; v <= 6; v++)
        {
            var env = MakeEnvelope(id, v);
            await _repository.SaveEventsAsync(new List<EventEnvelope> { env });
        }

        // Snapshot exists at version 5
        _snapshotMock
            .Setup(s => s.GetLatestSnapshotAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Shared.Results.Result<(string, long)>.Success(("{}", 5L)));

        var result = await _sut.CompactAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Data!.EventsRemoved.Should().Be(4); // versions 1-4 deleted
        result.Data.CompactedToVersion.Should().Be(5);

        var remaining = await _repository.GetEventsByAggregateIdAsync(id);
        remaining.Data!.Select(e => e.AggregateVersion).Should().BeEquivalentTo(new long[] { 5, 6 });
    }

    [Fact]
    public async Task CompactAsync_NoSnapshot_ReturnsFailure()
    {
        _snapshotMock
            .Setup(s => s.GetLatestSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Shared.Results.Result<(string, long)>.Failure("SNAPSHOT_NOT_FOUND", "No snapshot"));

        var result = await _sut.CompactAsync("agg-no-snap");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_SNAPSHOT");
    }

    [Fact]
    public async Task CompactAllAsync_SkipsAggregatesWithoutSnapshots()
    {
        const string withSnap = "agg-with";
        const string noSnap = "agg-without";

        for (long v = 1; v <= 4; v++)
            await _repository.SaveEventsAsync(new List<EventEnvelope> { MakeEnvelope(withSnap, v) });

        _snapshotMock
            .Setup(s => s.GetLatestSnapshotAsync(withSnap, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Shared.Results.Result<(string, long)>.Success(("{}", 3L)));

        _snapshotMock
            .Setup(s => s.GetLatestSnapshotAsync(noSnap, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Shared.Results.Result<(string, long)>.Failure("SNAPSHOT_NOT_FOUND", "No snapshot"));

        var result = await _sut.CompactAllAsync(new[] { withSnap, noSnap });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Should().HaveCount(1);
        result.Data[0].AggregateId.Should().Be(withSnap);
    }

    [Fact]
    public async Task CompactToVersionAsync_InvalidVersion_ReturnsFailure()
    {
        var result = await _sut.CompactToVersionAsync("agg-x", keepFromVersion: 0);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_VERSION");
    }
}
