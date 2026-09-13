#nullable enable
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
    /// <summary>
    /// Gets the total amount in the balance, including funds currently on hold.
    /// </summary>
    public Money CurrentAmount { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when the balance was last changed.
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Gets the number of completed fund additions and removals.
    /// </summary>
    public int TransactionCount { get; private set; }

    /// <summary>
    /// Gets the amount that is available for removal or placement on hold.
    /// </summary>
    public Money AvailableAmount { get; private set; }

    /// <summary>
    /// Gets the amount currently reserved by holds.
    /// </summary>
    public Money HoldAmount { get; private set; }

    /// <summary>
    /// Convenience alias for the ISO 4217 currency code of <see cref="CurrentAmount"/>.
    /// Excluded from serialization so snapshot payloads remain unchanged.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Currency => CurrentAmount.Currency;

    /// <summary>
    /// Initializes a new instance of the <see cref="Balance"/> class.
    /// </summary>
    /// <param name="initialAmount">The initial total and available amount.</param>
    /// <exception cref="ArgumentNullException"><paramref name="initialAmount"/> is <see langword="null"/>.</exception>
    public Balance(Money initialAmount)
    {
        CurrentAmount = initialAmount ?? throw new ArgumentNullException(nameof(initialAmount));
        AvailableAmount = initialAmount;
        HoldAmount = new Money(0, initialAmount.Currency);
        LastUpdated = DateTime.UtcNow;
        TransactionCount = 0;
    }

    /// <summary>
    /// Adds funds to the total and available amounts.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="amount"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">The currency of <paramref name="amount"/> differs from the balance currency.</exception>
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

    /// <summary>
    /// Removes funds from the total and available amounts.
    /// </summary>
    /// <param name="amount">The amount to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="amount"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">
    /// The currency of <paramref name="amount"/> differs from the balance currency, or the available amount is insufficient.
    /// </exception>
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

    /// <summary>
    /// Moves funds from the available amount to the amount on hold.
    /// </summary>
    /// <param name="amount">The amount to place on hold.</param>
    /// <exception cref="ArgumentNullException"><paramref name="amount"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">
    /// The currency of <paramref name="amount"/> differs from the balance currency, or the available amount is insufficient.
    /// </exception>
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

    /// <summary>
    /// Releases held funds back to the available amount.
    /// </summary>
    /// <param name="amount">The amount to release.</param>
    /// <exception cref="ArgumentNullException"><paramref name="amount"/> is <see langword="null"/>.</exception>
    /// <exception cref="DomainException">
    /// The currency of <paramref name="amount"/> differs from the hold currency, or the requested amount exceeds the amount on hold.
    /// </exception>
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

    /// <summary>
    /// Determines whether this balance has the same current, available, and held amounts as another balance.
    /// </summary>
    /// <param name="other">The balance to compare with this instance.</param>
    /// <returns><see langword="true"/> when the balances have equal amounts; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Balance? other)
    {
        if (other is null)
            return false;

        return CurrentAmount == other.CurrentAmount &&
               AvailableAmount == other.AvailableAmount &&
               HoldAmount == other.HoldAmount;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Balance);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(CurrentAmount, AvailableAmount, HoldAmount);

    /// <inheritdoc/>
    public override string ToString()
        => $"Balance {{ Current={CurrentAmount}, Available={AvailableAmount}, Hold={HoldAmount}, Transactions={TransactionCount} }}";
}
