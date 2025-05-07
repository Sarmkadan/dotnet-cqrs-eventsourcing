// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.ValueObjects;

using Shared.Exceptions;

/// <summary>
/// Value object representing an account balance with transaction tracking.
/// </summary>
public class Balance : IEquatable<Balance>
{
    public Money CurrentAmount { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public int TransactionCount { get; private set; }
    public Money AvailableAmount { get; private set; }
    public Money HoldAmount { get; private set; }

    public Balance(Money initialAmount)
    {
        CurrentAmount = initialAmount ?? throw new ArgumentNullException(nameof(initialAmount));
        AvailableAmount = initialAmount;
        HoldAmount = new Money(0, initialAmount.Currency);
        LastUpdated = DateTime.UtcNow;
        TransactionCount = 0;
    }

    public void AddFunds(Money amount)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount));

        if (!amount.Currency.Equals(CurrentAmount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot add funds in different currency.", "CURRENCY_MISMATCH");

        CurrentAmount = CurrentAmount.Add(amount);
        AvailableAmount = AvailableAmount.Add(amount);
        LastUpdated = DateTime.UtcNow;
        TransactionCount++;
    }

    public void RemoveFunds(Money amount)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount));

        if (!amount.Currency.Equals(CurrentAmount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot remove funds in different currency.", "CURRENCY_MISMATCH");

        if (AvailableAmount.IsLessThan(amount))
            throw new DomainException($"Insufficient available balance. Available: {AvailableAmount}, Requested: {amount}", "INSUFFICIENT_FUNDS");

        CurrentAmount = CurrentAmount.Subtract(amount);
        AvailableAmount = AvailableAmount.Subtract(amount);
        LastUpdated = DateTime.UtcNow;
        TransactionCount++;
    }

    public void PlaceHold(Money amount)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount));

        if (!amount.Currency.Equals(CurrentAmount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot place hold in different currency.", "CURRENCY_MISMATCH");

        if (AvailableAmount.IsLessThan(amount))
            throw new DomainException("Insufficient available balance for hold.", "INSUFFICIENT_FUNDS");

        AvailableAmount = AvailableAmount.Subtract(amount);
        HoldAmount = HoldAmount.Add(amount);
        LastUpdated = DateTime.UtcNow;
    }

    public void ReleaseHold(Money amount)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount));

        if (!amount.Currency.Equals(HoldAmount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Cannot release hold in different currency.", "CURRENCY_MISMATCH");

        if (HoldAmount.IsLessThan(amount))
            throw new DomainException("Hold amount to release exceeds current hold.", "INVALID_HOLD_RELEASE");

        HoldAmount = HoldAmount.Subtract(amount);
        AvailableAmount = AvailableAmount.Add(amount);
        LastUpdated = DateTime.UtcNow;
    }

    public bool Equals(Balance? other)
    {
        if (other is null)
            return false;

        return CurrentAmount == other.CurrentAmount &&
               AvailableAmount == other.AvailableAmount &&
               HoldAmount == other.HoldAmount;
    }

    public override bool Equals(object? obj) => Equals(obj as Balance);

    public override int GetHashCode() => HashCode.Combine(CurrentAmount, AvailableAmount, HoldAmount);

    public override string ToString()
        => $"Balance {{ Current={CurrentAmount}, Available={AvailableAmount}, Hold={HoldAmount}, Transactions={TransactionCount} }}";
}
