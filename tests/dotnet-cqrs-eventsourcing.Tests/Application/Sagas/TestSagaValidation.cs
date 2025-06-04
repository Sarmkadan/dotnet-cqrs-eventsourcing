using System;
using System.Collections.Generic;
using System.Linq;

// Adjusted namespace to match the existing test files
namespace DotNetCqrsEventSourcing.Tests.Application.Sagas
{
    /// <summary>
    /// Validation helpers for <see cref="TestSaga"/>.
    /// </summary>
    public static class TestSagaValidation
    {
        /// <summary>
        /// Returns a list of human‑readable validation problems for the supplied <see cref="TestSaga"/>.
        /// </summary>
        /// <param name="value">The saga instance to validate.</param>
        /// <returns>A read‑only list of problem descriptions. Empty if the saga is valid.</returns>
        public static IReadOnlyList<string> Validate(this TestSaga value)
        {
            var problems = new List<string>();

            if (value is null)
            {
                problems.Add("TestSaga instance is null.");
                return problems;
            }

            // HandledEvents should never be negative.
            if (value.HandledEvents < 0)
            {
                problems.Add($"HandledEvents must be non‑negative, but was {value.HandledEvents}.");
            }

            // Additional validation could be added here if more public state were exposed.

            return problems;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="TestSaga"/> is valid.
        /// </summary>
        /// <param name="value">The saga instance to check.</param>
        /// <returns>True if no validation problems are reported; otherwise false.</returns>
        public static bool IsValid(this TestSaga value) => !value.Validate().Any();

        /// <summary>
        /// Ensures the supplied <see cref="TestSaga"/> is valid, throwing an <see cref="ArgumentException"/>
        /// if any validation problems are found.
        /// </summary>
        /// <param name="value">The saga instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown when validation problems are present.</exception>
        public static void EnsureValid(this TestSaga value)
        {
            var problems = value.Validate();
            if (problems.Any())
            {
                throw new ArgumentException(
                    $"TestSaga validation failed: {string.Join("; ", problems)}",
                    nameof(value));
            }
        }
    }
}
