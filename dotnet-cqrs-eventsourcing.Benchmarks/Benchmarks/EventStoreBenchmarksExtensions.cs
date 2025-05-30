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
        /// <param name="aggregateId">The aggregate ID to use for testing</param>
        /// <param name="eventCount">Number of events to generate for replay benchmarks</param>
        /// <returns>A configured EventStoreBenchmarks instance</returns>
        public static EventStoreBenchmarks WithCustomParameters(this EventStoreBenchmarks benchmarks, string aggregateId, int eventCount)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));

            if (eventCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(eventCount), "Event count must be positive");

            // Store the custom parameters
            var customField = typeof(EventStoreBenchmarks).GetField("_testAggregateId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            customField?.SetValue(benchmarks, aggregateId);

            var eventsField = typeof(EventStoreBenchmarks).GetField("_generatedEvents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var events = (List<DomainEvent>)eventsField?.GetValue(benchmarks)!;

            events.Clear();
            for (int i = 0; i < eventCount; i++)
            {
                events.Add(new AccountCreatedEvent(
                    aggregateId,
                    $"ACC-{i:D6}",
                    $"Account Holder {i}",
                    "USD",
                    1000m + i,
                    DateTime.UtcNow.AddMinutes(-eventCount + i)
                ));
            }

            return benchmarks;
        }

        /// <summary>
        /// Runs all EventStore-related benchmarks and returns their results.
        /// Convenience method to execute a subset of benchmarks focused on event storage operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static async Task<Dictionary<string, object>> RunEventStoreBenchmarksAsync(this EventStoreBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var results = new Dictionary<string, object>(StringComparer.Ordinal);

            // Run EventStore append benchmarks
            await benchmarks.EventStore_AppendSingleEvent();
            results["EventStore_AppendSingleEvent"] = "Executed";

            await benchmarks.EventStore_AppendBatchOf100Events();
            results["EventStore_AppendBatchOf100Events"] = "Executed";

            // Run EventStore query benchmarks
            await benchmarks.EventStore_GetEventsByAggregateId();
            results["EventStore_GetEventsByAggregateId"] = "Executed";

            await benchmarks.EventStore_GetEventsFromVersion();
            results["EventStore_GetEventsFromVersion"] = "Executed";

            await benchmarks.EventStore_GetAggregateVersion();
            results["EventStore_GetAggregateVersion"] = "Executed";

            return results;
        }

        /// <summary>
        /// Runs all AggregateRoot-related benchmarks and returns their results.
        /// Convenience method to execute benchmarks focused on aggregate replay operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static async Task<Dictionary<string, object>> RunAggregateRootBenchmarksAsync(this EventStoreBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var results = new Dictionary<string, object>(StringComparer.Ordinal);

            // Run aggregate replay benchmarks
            benchmarks.AggregateRoot_Replay100Events();
            results["AggregateRoot_Replay100Events"] = "Executed";

            benchmarks.AggregateRoot_Replay1000Events();
            results["AggregateRoot_Replay1000Events"] = "Executed";

            benchmarks.AggregateRoot_Replay10000Events();
            results["AggregateRoot_Replay10000Events"] = "Executed";

            return results;
        }

        /// <summary>
        /// Runs all AccountService-related benchmarks and returns their results.
        /// Convenience method to execute benchmarks focused on account lifecycle operations.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        /// <returns>Dictionary mapping benchmark names to their results</returns>
        public static async Task<Dictionary<string, object>> RunAccountServiceBenchmarksAsync(this EventStoreBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var results = new Dictionary<string, object>(StringComparer.Ordinal);

            // Run account service benchmarks
            await benchmarks.AccountService_CreateAccount();
            results["AccountService_CreateAccount"] = "Executed";

            await benchmarks.AccountService_CompleteLifecycle();
            results["AccountService_CompleteLifecycle"] = "Executed";

            return results;
        }

        /// <summary>
        /// Disposes the benchmark service provider and cleans up resources.
        /// Provides a more explicit way to clean up compared to GlobalCleanup.
        /// </summary>
        /// <param name="benchmarks">The benchmarks instance</param>
        public static void DisposeServiceProvider(this EventStoreBenchmarks benchmarks)
        {
            if (benchmarks == null)
                throw new ArgumentNullException(nameof(benchmarks));

            var cleanupMethod = typeof(EventStoreBenchmarks).GetMethod("GlobalCleanup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cleanupMethod?.Invoke(benchmarks, null);
        }
    }
}