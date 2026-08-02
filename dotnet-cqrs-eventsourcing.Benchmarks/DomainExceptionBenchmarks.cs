using System;
using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Shared.Exceptions;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    /// <summary>
    /// Benchmarks for the <see cref="DomainException"/> type.
    /// </summary>
    [MemoryDiagnoser]
    public class DomainExceptionBenchmarks
    {
        /// <summary>
        /// Size of the exception message (in characters) used for the benchmarks.
        /// </summary>
        [Params(10, 100, 1000)]
        public int MessageSize { get; set; }

        private string _message = string.Empty;
        private DomainException _exception = null!;
        private string _json = string.Empty;

        /// <summary>
        /// Pre‑pares data used by the benchmark methods.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // Create a message of the requested size.
            _message = new string('x', MessageSize);

            // Create the exception instance.
            _exception = new DomainException(_message);

            // Serialize once – used by the deserialization benchmark.
            _json = _exception.ToJson();
        }

        /// <summary>
        /// Benchmarks the cost of constructing a <see cref="DomainException"/>.
        /// </summary>
        [Benchmark]
        public DomainException CreateException()
        {
            return new DomainException(_message);
        }

        /// <summary>
        /// Benchmarks the <c>ToString()</c> implementation.
        /// </summary>
        [Benchmark]
        public string ExceptionToString()
        {
            return _exception.ToString();
        }

        /// <summary>
        /// Benchmarks JSON serialization via the extension method.
        /// </summary>
        [Benchmark]
        public string SerializeToJson()
        {
            return _exception.ToJson();
        }

        /// <summary>
        /// Benchmarks JSON deserialization via the extension method.
        /// </summary>
        [Benchmark]
        public DomainException DeserializeFromJson()
        {
            // The extension method returns a nullable, but we know the JSON is valid.
            return DomainExceptionJsonExtensions.FromJson(_json)!;
        }
    }
}
