using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCqrsEventSourcing.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetCqrsEventSourcing.Tests.Infrastructure;

/// <summary>
/// Contains unit tests for <see cref="InMemoryCacheService"/> that verify
/// correct handling of expiration, eviction, and concurrent set/get operations.
/// </summary>
public class CacheServiceTests
{
    private readonly InMemoryCacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of <see cref="CacheServiceTests"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="InMemoryCacheService"/> is created with a <c>NullLogger</c>
    /// to avoid logging output during test execution.
    /// </remarks>
    public CacheServiceTests()
    {
        _cacheService = new InMemoryCacheService(NullLogger<InMemoryCacheService>.Instance);
    }

    /// <summary>
    /// Verifies that a value whose expiration has elapsed is not returned
    /// by <see cref="InMemoryCacheService.GetAsync{T}(string)"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes when the assertion has been performed.
    /// </returns>
    [Fact]
    public async Task GetAsync_ShouldNotReturnExpiredValue_DueToRaceCondition()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Set a value with 1 second expiration
        await _cacheService.SetAsync(key, value, TimeSpan.FromSeconds(1));

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Act - Get should return null for expired entry
        var result = await _cacheService.GetAsync<string>(key);

        // Assert - Should not return expired value
        Assert.Null(result);
    }

    /// <summary>
    /// Ensures that an expired cache entry is removed on the first retrieval
    /// and that subsequent retrievals also return <c>null</c>.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes when the assertions have been performed.
    /// </returns>
    [Fact]
    public async Task GetAsync_ShouldRemoveExpiredEntry_WhenFound()
    {
        // Arrange
        var key = "test-key-2";
        var value = "test-value-2";

        // Set a value with 1 second expiration
        await _cacheService.SetAsync(key, value, TimeSpan.FromSeconds(1));

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // First get should remove and return null
        var result1 = await _cacheService.GetAsync<string>(key);
        Assert.Null(result1);

        // Second get should also return null (entry should be gone)
        var result2 = await _cacheService.GetAsync<string>(key);
        Assert.Null(result2);
    }

    /// <summary>
    /// Tests that the internal eviction logic removes only the exact expired entry
    /// without affecting other valid entries.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes when the eviction and subsequent retrievals are verified.
    /// </returns>
    [Fact]
    public async Task EvictExpiredEntries_ShouldRemoveExactExpiredEntryObject()
    {
        // Arrange
        var key1 = "key1";
        var key2 = "key2";
        var value1 = "value1";
        var value2 = "value2";

        // Set two values with different expirations
        await _cacheService.SetAsync(key1, value1, TimeSpan.FromSeconds(1));
        await _cacheService.SetAsync(key2, value2, TimeSpan.FromSeconds(2));

        // Wait for first entry to expire
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Manually trigger eviction
        var method = typeof(InMemoryCacheService).GetMethod(
            "EvictExpiredEntries",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_cacheService, new[] { (object?)null });

        // Act - Get both values
        var result1 = await _cacheService.GetAsync<string>(key1);
        var result2 = await _cacheService.GetAsync<string>(key2);

        // Assert - First should be null (expired and evicted), second should have value
        Assert.Null(result1);
        Assert.Equal(value2, result2);
    }

    /// <summary>
    /// Confirms that a concurrent <c>SetAsync</c> operation is not overwritten
    /// by the removal of an expired entry when <c>GetAsync</c> is called.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes after the concurrent operations and verification.
    /// </returns>
    [Fact]
    public async Task GetAsync_ShouldNotClobberConcurrentSet_WhenEntryIsExpired()
    {
        // Arrange
        var key = "concurrent-key";
        var initialValue = "initial";
        var newValue = "new-value";

        // Set initial value with 1 second expiration
        await _cacheService.SetAsync(key, initialValue, TimeSpan.FromSeconds(1));

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Start a concurrent Set operation
        var setTask = _cacheService.SetAsync(key, newValue, TimeSpan.FromSeconds(10));

        // Immediately try to Get (this should not return the expired value)
        var getTask = _cacheService.GetAsync<string>(key);

        await Task.WhenAll(setTask, getTask);

        // Act - Get the value after both operations complete
        var result = await _cacheService.GetAsync<string>(key);

        // Assert - Should get the new value, not null
        // This tests that concurrent Set doesn't get clobbered by expired entry removal
        Assert.Equal(newValue, result);
    }

    /// <summary>
    /// Validates that <c>GetOrCreateAsync</c> correctly invokes the factory
    /// when the cached entry has expired, and that the newly created value is stored.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that completes after the factory invocation and result verification.
    /// </returns>
    [Fact]
    public async Task GetOrCreateAsync_ShouldHandleExpiredEntryCorrectly()
    {
        // Arrange
        var key = "get-or-create-key";
        var factoryCallCount = 0;

        Func<CancellationToken, Task<string>> factory = async ct =>
        {
            factoryCallCount++;
            await Task.Delay(10, ct);
            return "factory-value";
        };

        // Set initial value with 1 second expiration
        await _cacheService.SetAsync(key, "initial-value", TimeSpan.FromSeconds(1));

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // GetOrCreate should call factory since entry is expired
        var result = await _cacheService.GetOrCreateAsync(key, factory, TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal("factory-value", result);
        Assert.Equal(1, factoryCallCount);
    }
}
