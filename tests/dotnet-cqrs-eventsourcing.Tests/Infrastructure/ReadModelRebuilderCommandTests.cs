#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Infrastructure;

using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Infrastructure.Cli;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

public sealed class ReadModelRebuilderCommandTests
{
    private readonly Mock<IProjectionService> _projectionMock;
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly ReadModelRebuilderCommand _sut;

    public ReadModelRebuilderCommandTests()
    {
        _projectionMock = new Mock<IProjectionService>();
        _eventStoreMock = new Mock<IEventStore>();
        _sut = new ReadModelRebuilderCommand(
            _projectionMock.Object,
            _eventStoreMock.Object,
            NullLogger<ReadModelRebuilderCommand>.Instance);
    }

    [Fact]
    public void Name_IsExpectedValue()
    {
        _sut.Name.Should().Be("rebuild-read-models");
    }

    [Fact]
    public async Task ExecuteAsync_NoArgs_ReturnsFailureWithMissingArgument()
    {
        var result = await _sut.ExecuteAsync(Array.Empty<string>());
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("MISSING_ARGUMENT");
    }

    [Fact]
    public async Task ExecuteAsync_AllFlag_CallsRebuildAllProjectionsAsync()
    {
        _projectionMock
            .Setup(p => p.RebuildAllProjectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ExecuteAsync(new[] { "--all" });

        result.IsSuccess.Should().BeTrue();
        _projectionMock.Verify(p => p.RebuildAllProjectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AggregateFlag_CallsRebuildProjectionForSpecificAggregate()
    {
        _projectionMock
            .Setup(p => p.RebuildProjectionAsync("agg-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _sut.ExecuteAsync(new[] { "--aggregate", "agg-999" });

        result.IsSuccess.Should().BeTrue();
        _projectionMock.Verify(p => p.RebuildProjectionAsync("agg-999", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DryRunWithAllFlag_SkipsRebuildAndSucceeds()
    {
        var result = await _sut.ExecuteAsync(new[] { "--all", "--dry-run" });

        result.IsSuccess.Should().BeTrue();
        _projectionMock.Verify(p => p.RebuildAllProjectionsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _projectionMock.Verify(p => p.RebuildProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ProjectionServiceFails_PropagatesFailure()
    {
        _projectionMock
            .Setup(p => p.RebuildProjectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("REBUILD_ERROR", "Store unavailable"));

        var result = await _sut.ExecuteAsync(new[] { "--aggregate", "agg-fail" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("REBUILD_ERROR");
    }
}

public sealed class CliCommandRegistryTests
{
    [Fact]
    public async Task DispatchAsync_UnknownCommand_ReturnsFailure()
    {
        var registry = new CliCommandRegistry(
            Enumerable.Empty<ICliCommand>(),
            NullLogger<CliCommandRegistry>.Instance);

        var result = await registry.DispatchAsync(new[] { "unknown-cmd" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("UNKNOWN_COMMAND");
    }

    [Fact]
    public async Task DispatchAsync_KnownCommand_ExecutesAndReturnsSuccess()
    {
        var commandMock = new Mock<ICliCommand>();
        commandMock.Setup(c => c.Name).Returns("test-cmd");
        commandMock
            .Setup(c => c.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var registry = new CliCommandRegistry(
            new[] { commandMock.Object },
            NullLogger<CliCommandRegistry>.Instance);

        var result = await registry.DispatchAsync(new[] { "test-cmd", "--some-flag" });

        result.IsSuccess.Should().BeTrue();
        commandMock.Verify(c => c.ExecuteAsync(
            It.Is<string[]>(a => a.Length == 1 && a[0] == "--some-flag"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
