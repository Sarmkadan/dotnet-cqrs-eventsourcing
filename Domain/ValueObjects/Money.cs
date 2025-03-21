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
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.", "INVALID_AMOUNT")
                .WithMetadata("Amount", amount);

        if (amount > CqrsConstants.MaximumBalance)
            throw new DomainException($"Money amount exceeds maximum of {CqrsConstants.MaximumBalance}.", "AMOUNT_EXCEEDS_MAXIMUM")
                .WithMetadata("Amount", amount)
                .WithMetadata("Maximum", CqrsConstants.MaximumBalance);

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency code must be a valid 3-character ISO code.", "INVALID_CURRENCY_CODE")
                .WithMetadata("Currency", currency);

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Cannot add amounts in different currencies: {Currency} vs {other.Currency}.", "CURRENCY_MISMATCH");

        var newAmount = Amount + other.Amount;
        if (newAmount > CqrsConstants.MaximumBalance)
            throw new DomainException("Addition would exceed maximum balance.", "AMOUNT_EXCEEDS_MAXIMUM");

        return new Money(newAmount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"Cannot subtract amounts in different currencies: {Currency} vs {other.Currency}.", "CURRENCY_MISMATCH");

        var newAmount = Amount - other.Amount;
        if (newAmount < 0)
            throw new DomainException("Subtraction would result in negative amount.", "INSUFFICIENT_FUNDS");

        return new Money(newAmount, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot compare amounts in different currencies.", "CURRENCY_MISMATCH");

        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot compare amounts in different currencies.", "CURRENCY_MISMATCH");

        return Amount < other.Amount;
    }

    public bool Equals(Money? other)
    {
        if (other is null)
            return false;

        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Amount:F2} {Currency}";

    public static bool operator ==(Money left, Money right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(Money left, Money right) => !(left == right);
    public static bool operator <(Money left, Money right) => left.IsLessThan(right);
    public static bool operator >(Money left, Money right) => left.IsGreaterThan(right);
}
