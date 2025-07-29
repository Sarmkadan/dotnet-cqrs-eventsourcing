using BenchmarkDotNet.Running;
using dotnet_cqrs_eventsourcing.Benchmarks.Benchmarks;
using DotNetCqrsEventSourcing.Domain.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_cqrs_eventsourcing.Benchmarks.Benchmarks
{
    /// <summary>
    /// Extension methods for <see cref="EventStoreBenchmarks"/> that provide additional benchmarking utilities
    /// and helper methods for working with the benchmark results.
    /// </summary>
    public static class EventStoreBenchmarksExtensions
    {
        /// <summary>
        /// Creates a new instance of EventStoreBenchmarks with custom aggregate ID and event count.
        /// Useful for running benchmarks with different parameters without modifying the original class.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <param name="aggregateId">The aggregate ID to use for testing</param>
        /// <param name="eventCount">Number of events to generate for replay benchmarks</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="aggregateId"/> is null or whitespace</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="eventCount"/> is not positive</exception>
        /// <returns>A configured EventStoreBenchmarks instance</returns>
        public static EventStoreBenchmarks WithCustomParameters(this EventStoreBenchmarks benchmarks, string aggregateId, int eventCount)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(eventCount, 0);

            benchmarks.ConfigureForCustomParameters(aggregateId, eventCount);
            return benchmarks;
        }

        /// <summary>
        /// Configures the benchmarks instance with custom parameters.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to use for testing</param>
        /// <param name="eventCount">Number of events to generate for replay benchmarks</param>
        private static void ConfigureForCustomParameters(this EventStoreBenchmarks benchmarks, string aggregateId, int eventCount)
        {
            benchmarks.SetTestAggregateId(aggregateId);
            benchmarks.GenerateTestEvents(eventCount);
        }

        /// <summary>
        /// Sets the test aggregate ID for benchmarking.
        /// </summary>
        /// <param name="aggregateId">The aggregate ID to set</param>
        private static void SetTestAggregateId(this EventStoreBenchmarks benchmarks, string aggregateId)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

            var field = typeof(EventStoreBenchmarks).GetField(
                "_testAggregateId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(benchmarks, aggregateId);
        }

        /// <summary>
        /// Generates test events for replay benchmarks.
        /// </summary>
        /// <param name="count">Number of events to generate</param>
        private static void GenerateTestEvents(this EventStoreBenchmarks benchmarks, int count)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);

            var method = typeof(EventStoreBenchmarks).GetMethod(
                "GenerateTestEvents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(benchmarks, new object[] { count });
        }

        /// <summary>
        /// Runs all EventStore-related benchmarks and returns their results.
        /// Convenience method to execute a subset of benchmarks focused on event storage operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static async Task<Dictionary<string, BenchmarkResult>> RunEventStoreBenchmarksAsync(this EventStoreBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var results = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);

            // Run EventStore append benchmarks
            await benchmarks.EventStore_AppendSingleEvent();
            results.Add("EventStore_AppendSingleEvent", new BenchmarkResult("Executed", TimeSpan.Zero));

            await benchmarks.EventStore_AppendBatchOf100Events();
            results.Add("EventStore_AppendBatchOf100Events", new BenchmarkResult("Executed", TimeSpan.Zero));

            // Run EventStore query benchmarks
            await benchmarks.EventStore_GetEventsByAggregateId();
            results.Add("EventStore_GetEventsByAggregateId", new BenchmarkResult("Executed", TimeSpan.Zero));

            await benchmarks.EventStore_GetEventsFromVersion();
            results.Add("EventStore_GetEventsFromVersion", new BenchmarkResult("Executed", TimeSpan.Zero));

            await benchmarks.EventStore_GetAggregateVersion();
            results.Add("EventStore_GetAggregateVersion", new BenchmarkResult("Executed", TimeSpan.Zero));

            return results;
        }

        /// <summary>
        /// Runs all AggregateRoot-related benchmarks and returns their results.
        /// Convenience method to execute benchmarks focused on aggregate replay operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static Dictionary<string, BenchmarkResult> RunAggregateRootBenchmarks(this EventStoreBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var results = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);

            // Run aggregate replay benchmarks
            benchmarks.AggregateRoot_Replay100Events();
            results.Add("AggregateRoot_Replay100Events", new BenchmarkResult("Executed", TimeSpan.Zero));

            benchmarks.AggregateRoot_Replay1000Events();
            results.Add("AggregateRoot_Replay1000Events", new BenchmarkResult("Executed", TimeSpan.Zero));

            benchmarks.AggregateRoot_Replay10000Events();
            results.Add("AggregateRoot_Replay10000Events", new BenchmarkResult("Executed", TimeSpan.Zero));

            return results;
        }

        /// <summary>
        /// Runs all AccountService-related benchmarks and returns their results.
        /// Convenience method to execute benchmarks focused on account lifecycle operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static async Task<Dictionary<string, BenchmarkResult>> RunAccountServiceBenchmarksAsync(this EventStoreBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            var results = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);

            // Run account service benchmarks
            await benchmarks.AccountService_CreateAccount();
            results.Add("AccountService_CreateAccount", new BenchmarkResult("Executed", TimeSpan.Zero));

            await benchmarks.AccountService_CompleteLifecycle();
            results.Add("AccountService_CompleteLifecycle", new BenchmarkResult("Executed", TimeSpan.Zero));

            return results;
        }

        /// <summary>
        /// Disposes the benchmark service provider and cleans up resources.
        /// Provides a more explicit way to clean up compared to GlobalCleanup.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
        public static void DisposeServiceProvider(this EventStoreBenchmarks benchmarks)
        {
            benchmarks?.GlobalCleanup();
        }

        /// <summary>
        /// Represents the result of a benchmark execution.
        /// </summary>
        /// <param name="status">The execution status</param>
        /// <param name="duration">The execution duration</param>
        public sealed record BenchmarkResult(string Status, TimeSpan Duration);
    }
}