#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.ValueObjects;

using Shared.Constants;
using Shared.Exceptions;

/// <summary>
/// Value object representing a monetary amount with currency.
/// </summary>
public class Money : IEquatable<Money>
{
    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the ISO 4217 currency code in upper case.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> class with the specified amount and currency.
    /// </summary>
    /// <param name="amount">The monetary amount. Must not be negative or exceed the maximum balance.</param>
    /// <param name="currency">The three-letter ISO currency code.</param>
    /// <exception cref="DomainException">Thrown when the amount is negative, exceeds the maximum, or the currency code is invalid.</exception>
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.", "INVALID_AMOUNT")
                .WithMetadata("Amount", amount);
        }

        if (amount > CqrsConstants.MaximumBalance)
        {
            throw new DomainException($"Money amount exceeds maximum of {CqrsConstants.MaximumBalance}.", "AMOUNT_EXCEEDS_MAXIMUM")
                .WithMetadata("Amount", amount)
                .WithMetadata("Maximum", CqrsConstants.MaximumBalance);
        }

        ValidateCurrency(currency);

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Returns a new <see cref="Money"/> value equal to the sum of this amount and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The amount to add.</param>
    /// <returns>A new <see cref="Money"/> instance containing the sum.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">Thrown when the currencies differ.</exception>
    public Money Add(Money other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException($"Cannot add amounts in different currencies: {Currency} vs {other.Currency}.", "CURRENCY_MISMATCH");
        }

        var newAmount = Amount + other.Amount;

        return new Money(newAmount, Currency);
    }

    /// <summary>
    /// Returns a new <see cref="Money"/> value equal to this amount minus <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The amount to subtract.</param>
    /// <returns>A new <see cref="Money"/> instance containing the difference.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">Thrown when the currencies differ or the result would be negative.</exception>
    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException($"Cannot subtract amounts in different currencies: {Currency} vs {other.Currency}.", "CURRENCY_MISMATCH");
        }

        var newAmount = Amount - other.Amount;
        if (newAmount < 0)
        {
            throw new DomainException("Subtraction would result in negative amount.", "INSUFFICIENT_FUNDS");
        }

        return new Money(newAmount, Currency);
    }

    /// <summary>
    /// Determines whether this amount is greater than <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The amount to compare with.</param>
    /// <returns><see langword="true"/> if this amount is greater; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">Thrown when the currencies differ.</exception>
    public bool IsGreaterThan(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException("Cannot compare amounts in different currencies.", "CURRENCY_MISMATCH");
        }

        return Amount > other.Amount;
    }

    /// <summary>
    /// Determines whether this amount is less than <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The amount to compare with.</param>
    /// <returns><see langword="true"/> if this amount is less; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">Thrown when the currencies differ.</exception>
    public bool IsLessThan(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException("Cannot compare amounts in different currencies.", "CURRENCY_MISMATCH");
        }

        return Amount < other.Amount;
    }

    /// <summary>
    /// Determines whether this value equals another <see cref="Money"/> value by amount and currency.
    /// </summary>
    /// <param name="other">The value to compare with, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the amounts and currencies are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Money? other)
    {
        if (other is null)
        {
            return false;
        }

        ArgumentNullException.ThrowIfNull(other);
        return Amount == other.Amount && Currency == other.Currency;
    }

    /// <summary>
    /// Determines whether the specified object is a <see cref="Money"/> value equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if the object is equal to this instance; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => Equals(obj as Money);

    /// <summary>
    /// Returns a hash code based on the amount and currency.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    /// <summary>
    /// Returns a string representation of the amount with two decimal places followed by the currency code.
    /// </summary>
    /// <returns>A formatted string such as <c>10.00 USD</c>.</returns>
    public override string ToString() => $"{Amount:F2} {Currency}";

    /// <summary>
    /// Determines whether two <see cref="Money"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Money left, Money right) => left?.Equals(right) ?? right is null;

    /// <summary>
    /// Determines whether two <see cref="Money"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if the values are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Money left, Money right) => !(left == right);

    /// <summary>
    /// Determines whether the first <see cref="Money"/> value is less than the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(Money left, Money right) => left.IsLessThan(right);

    /// <summary>
    /// Determines whether the first <see cref="Money"/> value is greater than the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(Money left, Money right) => left.IsGreaterThan(right);

    private static void ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency code must be a valid 3-character ISO code.", "INVALID_CURRENCY_CODE")
                .WithMetadata("Currency", currency);
        }
    }
}
