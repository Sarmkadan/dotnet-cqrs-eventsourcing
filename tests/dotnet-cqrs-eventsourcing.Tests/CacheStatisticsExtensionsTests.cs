#nullable enable
using Xunit;
using DotNetCqrsEventSourcing.Infrastructure.Caching;

namespace DotNetCqrsEventSourcing.Tests;

public class CacheStatisticsExtensionsTests
{
    [Fact]
    public void HitRate_Returns_Correct_Value_When_Entries_Exist()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 100,
            TotalHits = 80,
            ExpiredEntries = 5,
            AverageEntryAge = TimeSpan.FromSeconds(10)
        };

        double hitRate = stats.HitRate();

        Assert.Equal(0.8, hitRate, precision: 5);
    }

    [Fact]
    public void HitRate_Returns_Zero_When_No_Entries()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 0,
            TotalHits = 0,
            ExpiredEntries = 0,
            AverageEntryAge = TimeSpan.Zero
        };

        double hitRate = stats.HitRate();

        Assert.Equal(0.0, hitRate);
    }

    [Fact]
    public void IsHealthy_Returns_True_When_HitRate_Meets_Threshold()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 50,
            TotalHits = 45,
            ExpiredEntries = 0,
            AverageEntryAge = TimeSpan.Zero
        };

        bool healthy = stats.IsHealthy(0.8);

        Assert.True(healthy);
    }

    [Fact]
    public void IsHealthy_Returns_False_When_HitRate_Below_Threshold()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 50,
            TotalHits = 30,
            ExpiredEntries = 0,
            AverageEntryAge = TimeSpan.Zero
        };

        bool healthy = stats.IsHealthy(0.8);

        Assert.False(healthy);
    }

    [Fact]
    public void ToDisplayString_Returns_Expected_Format()
    {
        var stats = new CacheStatistics
        {
            TotalEntries = 10,
            TotalHits = 7,
            ExpiredEntries = 2,
            AverageEntryAge = TimeSpan.FromSeconds(15)
        };

        string display = stats.ToDisplayString();

        Assert.Equal("TotalEntries: 10, TotalHits: 7, ExpiredEntries: 2, AverageEntryAge: 00:00:15", display);
    }
}
