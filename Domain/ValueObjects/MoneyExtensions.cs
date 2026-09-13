#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.ValueObjects;

using System;

/// <summary>
/// Extension methods for the <see cref="Money"/> value object.
/// </summary>
public static class MoneyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="Money"/> is zero.
    /// </summary>
    /// <param name="money">The money to check.</param>
    /// <returns>true if the amount is zero; otherwise, false.</returns>
    public static bool IsZero(this Money money)
    {
        if (money is null)
            throw new ArgumentNullException(nameof(money));

        return money.Amount == 0;
    }

    /// <summary>
    /// Determines whether the specified <see cref="Money"/> is positive.
    /// </summary>
    /// <param name="money">The money to check.</param>
    /// <returns>true if the amount is greater than zero; otherwise, false.</returns>
    public static bool IsPositive(this Money money)
    {
        if (money is null)
            throw new ArgumentNullException(nameof(money));

        return money.Amount > 0;
    }
}