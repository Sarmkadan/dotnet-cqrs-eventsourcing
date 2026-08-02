using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Shared.Exceptions;
using System;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    [MemoryDiagnoser]
    public class DotnetCqrsEventsourcingExceptionBenchmarks
    {
        private const string BaseMessage = "This is a test exception message for benchmarking.";
        private const string ErrorCode = "TEST_ERROR";

        [Params(10, 100, 1000)]
        public int MessageLength { get; set; }

        private string _longMessage = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Create a long message for the message size impact test
            _longMessage = new string('A', MessageLength);
        }

        [Benchmark]
        public void Throw_Exception()
        {
            try
            {
                throw new DotnetCqrsEventsourcingException(BaseMessage, ErrorCode);
            }
            catch (DotnetCqrsEventsourcingException)
            {
                // Intentionally empty
            }
        }

        [Benchmark]
        public void Catch_And_Rethrow_Exception()
        {
            try
            {
                try
                {
                    throw new DotnetCqrsEventsourcingException(BaseMessage, ErrorCode);
                }
                catch (DotnetCqrsEventsourcingException ex)
                {
                    // Rethrow preserving the original exception information
                    throw;
                }
            }
            catch (DotnetCqrsEventsourcingException)
            {
                // Intentionally empty
            }
        }

        [Benchmark]
        public void Throw_Exception_With_Large_Message()
        {
            try
            {
                throw new DotnetCqrsEventsourcingException(_longMessage, ErrorCode);
            }
            catch (DotnetCqrsEventsourcingException)
            {
                // Intentionally empty
            }
        }

        [Benchmark]
        public string ToString_Invocation()
        {
            var ex = new DotnetCqrsEventsourcingException(BaseMessage, ErrorCode);
            return ex.ToString();
        }
    }
}