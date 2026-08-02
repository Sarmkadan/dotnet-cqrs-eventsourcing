using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using DotNetCqrsEventSourcing.Domain.ValueObjects;

namespace DotNetCqrsEventSourcing.Benchmarks
{
    /// <summary>
    /// Benchmarks for the public methods of <see cref="Money"/> that are exercised by the
    /// <c>MoneyTestsValidation</c> test suite.
    /// </summary>
    [MemoryDiagnoser]
    public class MoneyTestsValidationBenchmarks
    {
        /// <summary>
        /// Number of <see cref="Money"/> instances used in the benchmarks.
        /// </summary>
        [Params(10, 100, 1000)]
        public int Count { get; set; }

        private List<Money> _moneyList;
        private Money _singleMoney;
        private Money _otherMoney;

        /// <summary>
        /// Creates a list of <see cref="Money"/> objects with varying amounts and a couple of
        /// reference instances used by the benchmark methods.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _moneyList = new List<Money>(Count);
            for (int i = 0; i < Count; i++)
            {
                // Vary the amount to avoid constant folding.
                decimal amount = i + 1m;
                _moneyList.Add(new Money(amount, "USD"));
            }

            // Instances used for comparison benchmarks.
            _singleMoney = new Money(123.45m, "USD");
            _otherMoney = new Money(100m, "USD");
        }

        /// <summary>
        /// Benchmarks the <see cref="Money.Add(Money)"/> method by aggregating a collection of
        /// <see cref="Money"/> values.
        /// </summary>
        [Benchmark]
        public Money Add_Many()
        {
            Money result = new Money(0m, "USD");
            foreach (var money in _moneyList)
            {
                result = result.Add(money);
            }

            return result;
        }

        /// <summary>
        /// Benchmarks the <see cref="Money.IsGreaterThan(Money)"/> method across a collection.
        /// </summary>
        [Benchmark]
        public bool IsGreaterThan_Many()
        {
            bool any = false;
            foreach (var money in _moneyList)
            {
                any |= _singleMoney.IsGreaterThan(money);
            }

            return any;
        }

        /// <summary>
        /// Benchmarks the <see cref="Money.IsLessThan(Money)"/> method across a collection.
        /// </summary>
        [Benchmark]
        public bool IsLessThan_Many()
        {
            bool any = false;
            foreach (var money in _moneyList)
            {
                any |= _singleMoney.IsLessThan(money);
            }

            return any;
        }

        /// <summary>
        /// Benchmarks the <see cref="Money.ToString()"/> method for a collection of values.
        /// </summary>
        [Benchmark]
        public string ToString_Many()
        {
            var sb = new StringBuilder();
            foreach (var money in _moneyList)
            {
                sb.Append(money.ToString());
            }

            return sb.ToString();
        }
    }
}
