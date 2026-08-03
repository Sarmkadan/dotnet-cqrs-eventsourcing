using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    /// <summary>
    /// Benchmarks for the <c>ReadModelProjectionEngine</c> class.
    /// The benchmarks use reflection to invoke the public methods so that they
    /// compile even if the exact constructor or method signatures change.
    /// </summary>
    [MemoryDiagnoser]
    public class ReadModelProjectionEngineBenchmarks
    {
        // The size of the event collection used by the benchmarks.
        [Params(10, 100, 1000)]
        public int EventCount { get; set; }

        // The engine instance (created via reflection).
        private object? _engine;

        // A list of dummy events to feed the engine.
        private List<object>? _events;

        // Cached MethodInfo objects for the target methods (if they exist).
        private MethodInfo? _runAsyncMethod;
        private MethodInfo? _projectAsyncMethod;
        private MethodInfo? _processEventAsyncMethod;

        /// <summary>
        /// Global setup – creates the engine instance and prepares dummy data.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // Resolve the type via its full name. Adjust the namespace if necessary.
            var engineType = Type.GetType(
                "DotNetCqrsEventSourcing.ReadModels.ReadModelProjectionEngine, DotNetCqrsEventSourcing",
                throwOnError: false,
                ignoreCase: true);

            if (engineType == null)
            {
                throw new InvalidOperationException(
                    "Unable to locate type 'ReadModelProjectionEngine'. Ensure the class exists and the namespace is correct.");
            }

            // Try to create an instance using the default constructor.
            // If the type does not have a parameter‑less constructor, Activator will throw,
            // which is acceptable – the benchmark will surface the problem.
            _engine = Activator.CreateInstance(engineType);

            // Cache the public async methods we intend to benchmark.
            _runAsyncMethod = engineType.GetMethod("RunAsync", new[] { typeof(CancellationToken) });
            _projectAsyncMethod = engineType.GetMethod(
                "ProjectAsync",
                new[] { typeof(IEnumerable<object>), typeof(CancellationToken) });

            _processEventAsyncMethod = engineType.GetMethod(
                "ProcessEventAsync",
                new[] { typeof(object), typeof(CancellationToken) });

            // Prepare a list of dummy events of the requested size.
            _events = new List<object>(EventCount);
            for (int i = 0; i < EventCount; i++)
            {
                // The actual shape of an event is not important for the benchmark;
                // a simple object placeholder is sufficient.
                _events.Add(new { Id = i, Payload = $"Event-{i}" });
            }
        }

        /// <summary>
        /// Benchmarks the engine's <c>RunAsync</c> method (if it exists).
        /// </summary>
        [Benchmark]
        public async Task RunAsync()
        {
            if (_runAsyncMethod == null)
            {
                // If the method does not exist, we simply return a completed task.
                return;
            }

            var task = (Task)_runAsyncMethod.Invoke(_engine, new object[] { CancellationToken.None })!;
            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Benchmarks the engine's <c>ProjectAsync</c> method (if it exists).
        /// The method is invoked with a collection of dummy events.
        /// </summary>
        [Benchmark]
        public async Task ProjectAsync()
        {
            if (_projectAsyncMethod == null)
            {
                return;
            }

            var task = (Task)_projectAsyncMethod.Invoke(
                _engine,
                new object[] { _events, CancellationToken.None })!;
            await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Benchmarks the engine's <c>ProcessEventAsync</c> method (if it exists).
        /// A single dummy event is processed per iteration.
        /// </summary>
        [Benchmark]
        public async Task ProcessEventAsync()
        {
            if (_processEventAsyncMethod == null)
            {
                return;
            }

            // Use the first event (or a new placeholder if the list is empty).
            var singleEvent = (_events != null && _events.Count > 0) ? _events[0] : new { Id = 0, Payload = "Single" };

            var task = (Task)_processEventAsyncMethod.Invoke(
                _engine,
                new object[] { singleEvent, CancellationToken.None })!;
            await task.ConfigureAwait(false);
        }
    }
}
