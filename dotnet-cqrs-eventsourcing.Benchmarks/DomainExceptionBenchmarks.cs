using System;
using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;
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
        /// Serializable snapshot of a <see cref="DomainException"/>.
        /// The exception itself has no parameterless constructor, so it cannot be deserialized directly.
        /// </summary>
        public sealed record DomainExceptionDto(string Message, string Code, Dictionary<string, object> Metadata);

        /// <summary>
        /// Size of the exception message (in characters) used for the benchmarks.
        /// </summary>
        [Params(10, 100, 1000)]
        public int MessageSize { get; set; }

        private string _message = string.Empty;
        private DomainException _exception = null!;
        private string _json = string.Empty;

        /// <summary>
        /// Prepares data used by the benchmark methods.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // Create a message of the requested size.
            _message = new string('x', MessageSize);

            // Create the exception instance.
            _exception = new DomainException(_message);

            // Serialize once – used by the deserialization benchmark.
            _json = SerializeToJsonInternal(_exception);
        }

        private static string SerializeToJsonInternal(DomainException exception)
        {
            var dto = new DomainExceptionDto(exception.Message, exception.Code, exception.Metadata);
            return SerializationUtilities.ToJson(dto);
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
            return SerializeToJsonInternal(_exception);
        }

        /// <summary>
        /// Benchmarks JSON deserialization via <see cref="SerializationUtilities"/>.
        /// </summary>
        [Benchmark]
        public DomainExceptionDto DeserializeFromJson()
        {
            // The helper returns a nullable, but we know the JSON is valid.
            return SerializationUtilities.FromJson<DomainExceptionDto>(_json)!;
        }
    }
}
