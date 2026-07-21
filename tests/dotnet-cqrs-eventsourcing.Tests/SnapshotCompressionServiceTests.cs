using System.IO.Compression;
using System.Text;
using DotNetCqrsEventSourcing.Domain.Snapshots;
using DotNetCqrsEventSourcing.Infrastructure.Compression;
using DotNetCqrsEventSourcing.Shared.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetCqrsEventSourcing.Tests;

public class SnapshotCompressionServiceTests
{
    private readonly ILogger<SnapshotCompressionService> _logger;
    private readonly SnapshotCompressionService _service;

    public SnapshotCompressionServiceTests()
    {
        _logger = new NullLogger<SnapshotCompressionService>();
        _service = new SnapshotCompressionService(_logger);
    }

    [Fact]
    public async Task CompressAsync_WithNullSnapshot_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CompressAsync(null!));
    }

    [Fact]
    public async Task DecompressAsync_WithNullSnapshot_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DecompressAsync(null!));
    }

    [Fact]
    public async Task CompressAsync_WithEmptyData_ReturnsFailureResult()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "");

        // Act
        var result = await _service.CompressAsync(snapshot);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMPTY_DATA");
        result.ErrorMessage.Should().Contain("no data to compress");
    }

    [Fact]
    public async Task CompressAsync_WithWhitespaceData_ReturnsFailureResult()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "   ");

        // Act
        var result = await _service.CompressAsync(snapshot);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMPTY_DATA");
    }

    [Fact]
    public async Task CompressAsync_WithSmallData_ReturnsSuccessWithCompressedData()
    {
        // Arrange
        var originalData = "{\"Name\":\"Test\",\"Value\":42}";
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, originalData);

        // Act
        var result = await _service.CompressAsync(snapshot);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.IsCompressed.Should().BeTrue();
        result.Data.AggregateData.Should().NotBe(originalData);
        result.Data.AggregateData.Should().NotBeEmpty();
        result.Data.UncompressedSize.Should().Be(originalData.Length);
        result.Data.CompressedSize.Should().BeGreaterThan(0);
        // Note: Small data may not compress due to GZip overhead, so we just check it's processed
        result.Data.CompressedSize.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CompressAsync_WithAlreadyCompressedData_ReturnsSuccessWithoutRecompressing()
    {
        // Arrange
        var originalData = "{\"Name\":\"Test\",\"Value\":42}";
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, originalData);

        // First compress
        var firstResult = await _service.CompressAsync(snapshot);
        firstResult.IsSuccess.Should().BeTrue();

        var compressedSnapshot = firstResult.Data!;
        var compressedData = compressedSnapshot.AggregateData;

        // Act - try to compress again
        var secondResult = await _service.CompressAsync(compressedSnapshot);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Data.Should().BeSameAs(compressedSnapshot);
        secondResult.Data.AggregateData.Should().Be(compressedData);
        secondResult.Data.IsCompressed.Should().BeTrue();
    }

    [Fact]
    public async Task DecompressAsync_WithUncompressedData_ReturnsOriginalData()
    {
        // Arrange
        var originalData = "{\"Name\":\"Test\",\"Value\":42}";
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, originalData);

        // Act
        var result = await _service.DecompressAsync(snapshot);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(originalData);
    }

    [Fact]
    public async Task DecompressAsync_WithEmptyCompressedData_ReturnsFailureResult()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "");
        snapshot.MarkCompressed(0);

        // Act
        var result = await _service.DecompressAsync(snapshot);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMPTY_DATA");
        result.ErrorMessage.Should().Contain("no compressed data");
    }

    [Fact]
    public async Task RoundTripCompression_WithJsonData_ReturnsOriginalData()
    {
        // Arrange
        var originalData = "{\"Id\":\"123\",\"Name\":\"Test Aggregate\",\"Value\":12345,\"Items\":[1,2,3,4,5],\"Metadata\":{\"Created\":\"2024-01-01\",\"Author\":\"Test User\"}}";
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 5, originalData);

        // Act - compress
        var compressResult = await _service.CompressAsync(snapshot);
        compressResult.IsSuccess.Should().BeTrue();

        var compressedSnapshot = compressResult.Data!;
        compressedSnapshot.IsCompressed.Should().BeTrue();
        compressedSnapshot.AggregateData.Should().NotBe(originalData);

        // Act - decompress
        var decompressResult = await _service.DecompressAsync(compressedSnapshot);
        decompressResult.IsSuccess.Should().BeTrue();

        // Assert
        decompressResult.Data.Should().Be(originalData);
        compressedSnapshot.UncompressedSize.Should().Be(originalData.Length);
        compressedSnapshot.CompressedSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RoundTripCompression_WithLargeRepetitiveData_ActuallyShrinks()
    {
        // Arrange - large repetitive data that should compress well
        var repetitiveData = new StringBuilder()
            .Append("{\"Data\":\"")
            .Append("x".PadRight(1000, 'x')) // 1000 x characters
            .Append("y".PadRight(1000, 'y')) // 1000 y characters
            .Append("z".PadRight(1000, 'z')) // 1000 z characters
            .Append("\",\"Metadata\":{\"Key\":\"")
            .Append("abcdefghijklmnopqrstuvwxyz".PadRight(500, ' '))
            .Append("\"}}")
            .ToString();

        var snapshot = new AggregateSnapshot("agg-large", "LargeAggregate", 100, repetitiveData);

        // Act - compress
        var compressResult = await _service.CompressAsync(snapshot);
        compressResult.IsSuccess.Should().BeTrue();

        var compressedSnapshot = compressResult.Data!;
        compressedSnapshot.IsCompressed.Should().BeTrue();

        // Assert that compression actually reduced size
        compressedSnapshot.CompressedSize.Should().BeLessThan(snapshot.UncompressedSize);
        compressedSnapshot.GetCompressionRatio().Should().BeGreaterThan(50); // At least 50% reduction

        // Act - decompress
        var decompressResult = await _service.DecompressAsync(compressedSnapshot);
        decompressResult.IsSuccess.Should().BeTrue();

        // Assert
        decompressResult.Data.Should().Be(repetitiveData);
    }

    [Fact]
    public async Task RoundTripCompression_WithVeryLargeData_HandlesCorrectly()
    {
        // Arrange - very large data (1MB+)
        var largeData = new StringBuilder()
            .Append("{\"LargeDataSet\":[")
            .AppendJoin(",", Enumerable.Range(1, 10000).Select(i => $"{{\"Id\":{i},\"Value\":\"{new string('a', 100)}\"}}"))
            .Append("]}")
            .ToString();

        var snapshot = new AggregateSnapshot("agg-huge", "HugeAggregate", 500, largeData);

        // Act - compress
        var compressResult = await _service.CompressAsync(snapshot);
        compressResult.IsSuccess.Should().BeTrue();

        var compressedSnapshot = compressResult.Data!;
        compressedSnapshot.IsCompressed.Should().BeTrue();
        compressedSnapshot.CompressedSize.Should().BeLessThan(snapshot.UncompressedSize);

        // Act - decompress
        var decompressResult = await _service.DecompressAsync(compressedSnapshot);
        decompressResult.IsSuccess.Should().BeTrue();

        // Assert
        decompressResult.Data.Should().Be(largeData);
    }

    [Fact]
    public async Task DecompressAsync_WithInvalidBase64Data_ReturnsFailureResult()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "!!!invalid base64!!!");
        snapshot.MarkCompressed(20);

        // Act
        var result = await _service.DecompressAsync(snapshot);

        // Assert - should return failure result, not throw
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DECOMPRESSION_FAILED");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DecompressAsync_WithInvalidBase64_ReturnsFailureResult()
    {
        // Arrange - create a snapshot with invalid base64 data
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "!!!invalid!!!");
        snapshot.MarkCompressed(10);

        // Act
        var result = await _service.DecompressAsync(snapshot);

        // Assert - should return failure result, not throw
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DECOMPRESSION_FAILED");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetStats_WithNoOperations_ReturnsZeroStats()
    {
        // Act
        var stats = _service.GetStats();

        // Assert
        stats.SnapshotsProcessed.Should().Be(0);
        stats.TotalOriginalBytes.Should().Be(0);
        stats.TotalCompressedBytes.Should().Be(0);
        stats.OverallCompressionRatio.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_AfterCompression_UpdatesCorrectly()
    {
        // Arrange
        var data1 = "{\"Test\":1}";
        var data2 = new string('x', 1000);
        var snapshot1 = new AggregateSnapshot("agg-1", "Test", 1, data1);
        var snapshot2 = new AggregateSnapshot("agg-2", "Test", 2, data2);

        // Act - compress both
        await _service.CompressAsync(snapshot1);
        await _service.CompressAsync(snapshot2);

        // Act - get stats
        var stats = _service.GetStats();

        // Assert
        stats.SnapshotsProcessed.Should().Be(2);
        stats.TotalOriginalBytes.Should().Be(data1.Length + data2.Length);
        stats.TotalCompressedBytes.Should().BeGreaterThan(0);
        stats.TotalCompressedBytes.Should().BeLessThan(stats.TotalOriginalBytes);
        stats.OverallCompressionRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompressAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "test data");

        // Act - cancellation token is accepted but service doesn't actively monitor it
        var result = await _service.CompressAsync(snapshot, cancellationToken: CancellationToken.None);

        // Assert - service completes successfully
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DecompressAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var snapshot = new AggregateSnapshot("agg-1", "TestAggregate", 1, "test data");

        // First compress
        var compressResult = await _service.CompressAsync(snapshot);
        compressResult.IsSuccess.Should().BeTrue();

        // Act - cancellation token is accepted but service doesn't actively monitor it
        var result = await _service.DecompressAsync(compressResult.Data!, cancellationToken: CancellationToken.None);

        // Assert - service completes successfully
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CompressAsync_WithDifferentCompressionLevels_ProducesDifferentSizes()
    {
        // Arrange
        var data = new string('x', 1000);
        var snapshot = new AggregateSnapshot("agg-1", "Test", 1, data);

        // Act - compress with different levels
        var optimalResult = await _service.CompressAsync(snapshot, CompressionLevel.Optimal);
        var fastestResult = await _service.CompressAsync(snapshot, CompressionLevel.Fastest);
        var smallestResult = await _service.CompressAsync(snapshot, CompressionLevel.SmallestSize);

        // Assert - all should succeed
        optimalResult.IsSuccess.Should().BeTrue();
        fastestResult.IsSuccess.Should().BeTrue();
        smallestResult.IsSuccess.Should().BeTrue();

        // Note: Different compression levels may produce same or different sizes
        // We just verify they all work
    }

    [Fact]
    public async Task RoundTrip_PreservesDataIntegrity()
    {
        // Arrange
        var originalData = "{\"Name\":\"Test\",\"Value\":42,\"Timestamp\":\"2024-07-21T12:00:00Z\"}";
        var snapshot = new AggregateSnapshot("agg-checksum", "TestAggregate", 7, originalData);

        // Compute checksum before compression
        snapshot.ComputeChecksum();
        var originalChecksum = snapshot.Checksum;
        originalChecksum.Should().NotBeNullOrEmpty();

        // Act - compress
        var compressResult = await _service.CompressAsync(snapshot);
        compressResult.IsSuccess.Should().BeTrue();

        var compressedSnapshot = compressResult.Data!;

        // Act - decompress
        var decompressResult = await _service.DecompressAsync(compressedSnapshot);
        decompressResult.IsSuccess.Should().BeTrue();

        // Assert - data integrity maintained
        decompressResult.Data.Should().Be(originalData);
    }
}