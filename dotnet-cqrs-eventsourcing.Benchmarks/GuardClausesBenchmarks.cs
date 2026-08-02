using System;
using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    /// <summary>
    /// Benchmarks for the public methods of <see cref="GuardClauses"/>.
    /// </summary>
    [MemoryDiagnoser]
    public class GuardClausesBenchmarks
    {
        // The size of the data set used for the benchmarks.
        // 10, 100 and 1 000 give a quick view of scaling behaviour.
        [Params(10, 100, 1000)]
        public int Size { get; set; }

        private int[] _intValues;
        private long[] _longValues;
        private Guid[] _guids;
        private string[] _strings;
        private const string _pattern = @"^\d+$";

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Populate arrays with deterministic data.
            _intValues = new int[Size];
            _longValues = new long[Size];
            _guids = new Guid[Size];
            _strings = new string[Size];

            for (int i = 0; i < Size; i++)
            {
                // Alternate between zero and non‑zero values to keep the guard active.
                _intValues[i] = i % 2 == 0 ? i + 1 : 0;
                _longValues[i] = i % 2 == 0 ? i + 1L : 0L;

                // Use Guid.Empty for half the entries to trigger the guard.
                _guids[i] = i % 2 == 0 ? Guid.NewGuid() : Guid.Empty;

                // Create numeric strings for the Matches benchmark.
                _strings[i] = (i % 2 == 0 ? i + 1 : 0).ToString();
            }
        }

        /// <summary>
        /// Benchmarks GuardClauses.NotZero for <see cref="int"/>.
        /// </summary>
        [Benchmark]
        public void NotZeroInt()
        {
            for (int i = 0; i < Size; i++)
            {
                GuardClauses.NotZero(_intValues[i], nameof(_intValues));
            }
        }

        /// <summary>
        /// Benchmarks GuardClauses.NotZero for <see cref="long"/>.
        /// </summary>
        [Benchmark]
        public void NotZeroLong()
        {
            for (int i = 0; i < Size; i++)
            {
                GuardClauses.NotZero(_longValues[i], nameof(_longValues));
            }
        }

        /// <summary>
        /// Benchmarks GuardClauses.Condition.
        /// </summary>
        [Benchmark]
        public void ConditionCheck()
        {
            for (int i = 0; i < Size; i++)
            {
                GuardClauses.Condition(i % 2 == 0, "condition must be true");
            }
        }

        /// <summary>
        /// Benchmarks GuardClauses.NotEmpty for <see cref="Guid"/>.
        /// </summary>
        [Benchmark]
        public void NotEmptyGuid()
        {
            for (int i = 0; i < Size; i++)
            {
                GuardClauses.NotEmpty(_guids[i], nameof(_guids));
            }
        }

        /// <summary>
        /// Benchmarks GuardClauses.Matches.
        /// </summary>
        [Benchmark]
        public void MatchesPattern()
        {
            for (int i = 0; i < Size; i++)
            {
                GuardClauses.Matches(_strings[i], _pattern, nameof(_strings));
            }
        }
    }
}
