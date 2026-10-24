#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Shared;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// Argument guard clauses shared by every layer. Public entry points call these first, before any
/// <c>try</c> block, so invalid input fails immediately with a named exception instead of surfacing
/// later as a <see cref="NullReferenceException"/> or a silent <c>Result</c> failure.
/// <para>
/// Null reference arguments raise <see cref="ArgumentNullException"/>. Empty, blank, or null-element
/// collection input raises <see cref="ArgumentException"/>. Helpers that return a value return the
/// checked argument, so a guard can be used inline: <c>_repository = Guard.NotNull(repository);</c>.
/// The parameter name is captured from the call site, so the exception always names the argument.
/// </para>
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures that a reference-type argument is not <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The reference type of the argument.</typeparam>
    /// <param name="value">The argument to check.</param>
    /// <param name="paramName">The argument name, captured from the call site when omitted.</param>
    /// <returns>The checked, non-null <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Ensures that a string argument is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">The argument name, captured from the call site when omitted.</param>
    /// <returns>The checked, non-empty <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static string NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }

    /// <summary>
    /// Ensures that a string argument is neither <see langword="null"/> nor empty or whitespace-only.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="paramName">The argument name, captured from the call site when omitted.</param>
    /// <returns>The checked <paramref name="value"/>, which contains at least one non-whitespace character.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static string NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    /// <summary>
    /// Ensures that a collection argument is not <see langword="null"/> and contains no <see langword="null"/> elements.
    /// </summary>
    /// <typeparam name="T">The reference type of the collection elements.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="paramName">The argument name, captured from the call site when omitted.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="items"/> contains a <see langword="null"/> element.</exception>
    public static void NoNullElements<T>(IEnumerable<T?> items, [CallerArgumentExpression(nameof(items))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(items, paramName);

        if (items.Any(static item => item is null))
        {
            throw new ArgumentException("Collection cannot contain null elements.", paramName);
        }
    }
}
