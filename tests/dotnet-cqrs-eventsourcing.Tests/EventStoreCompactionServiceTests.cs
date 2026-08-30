#nullable enable

namespace DotNetCqrsEventSourcing.Tests;

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public sealed class EventStoreCompactionServiceTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ISnapshotService> _snapshotServiceMock = new();
    private readonly ILogger<EventStoreCompactionService> _logger =
        NullLogger<EventStoreCompactionService>.Instance;

    [Fact]
    public void Constructor_NullEventRepository_ThrowsArgumentNullException()
    {
        var act = () => new EventStoreCompactionService(
            null!,
            _snapshotServiceMock.Object,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventRepository");
    }

    [Fact]
    public void Constructor_NullSnapshotService_ThrowsArgumentNullException()
    {
        var act = () => new EventStoreCompactionService(
            _eventRepositoryMock.Object,
            null!,
            _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snapshotService");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EventStoreCompactionService(
            _eventRepositoryMock.Object,
            _snapshotServiceMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CompactAsync_NullOrEmptyAggregateId_ThrowsArgumentException(string? aggregateId)
    {
        var sut = CreateSut();

        var act = () => sut.CompactAsync(aggregateId!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("aggregateId");
    }

    [Fact]
    public async Task CompactAsync_SnapshotLookupFails_ReturnsNoSnapshotFailure()
    {
        const string aggregateId = "aggregate-1";
        _snapshotServiceMock
            .Setup(service => service.GetLatestSnapshotAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string AggregateData, long Version)>.Failure(
                "SNAPSHOT_NOT_FOUND",
                "No snapshot exists."));
        var sut = CreateSut();

        var result = await sut.CompactAsync(aggregateId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_SNAPSHOT");
        _eventRepositoryMock.Verify(
            repository => repository.DeleteEventsBeforeVersionAsync(
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompactAsync_SnapshotExists_DeletesEventsAtSnapshotVersionAndReturnsSuccess()
    {
        const string aggregateId = "aggregate-2";
        const long snapshotVersion = 12;
        const int deletedEvents = 7;
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        _snapshotServiceMock
            .Setup(service => service.GetLatestSnapshotAsync(aggregateId, cancellationToken))
            .ReturnsAsync(Result<(string AggregateData, long Version)>.Success(("{}", snapshotVersion)));
        _eventRepositoryMock
            .Setup(repository => repository.DeleteEventsBeforeVersionAsync(
                aggregateId,
                snapshotVersion,
                cancellationToken))
            .ReturnsAsync(Result<int>.Success(deletedEvents));
        var sut = CreateSut();

        var result = await sut.CompactAsync(aggregateId, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AggregateId.Should().Be(aggregateId);
        result.Data.EventsRemoved.Should().Be(deletedEvents);
        result.Data.CompactedToVersion.Should().Be(snapshotVersion);
        _eventRepositoryMock.Verify(
            repository => repository.DeleteEventsBeforeVersionAsync(
                aggregateId,
                snapshotVersion,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CompactAsync_RepositoryDeletionFails_PropagatesFailure()
    {
        const string aggregateId = "aggregate-3";
        const long snapshotVersion = 8;
        _snapshotServiceMock
            .Setup(service => service.GetLatestSnapshotAsync(aggregateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<(string AggregateData, long Version)>.Success(("{}", snapshotVersion)));
        _eventRepositoryMock
            .Setup(repository => repository.DeleteEventsBeforeVersionAsync(
                aggregateId,
                snapshotVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("DELETE_FAILED", "Unable to delete events."));
        var sut = CreateSut();

        var result = await sut.CompactAsync(aggregateId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DELETE_FAILED");
        result.ErrorMessage.Should().Be("Unable to delete events.");
    }

    private EventStoreCompactionService CreateSut() =>
        new(_eventRepositoryMock.Object, _snapshotServiceMock.Object, _logger);
}
