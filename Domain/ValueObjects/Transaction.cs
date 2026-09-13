#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.ValueObjects;

using Shared.Enums;

/// <summary>
/// Value object representing a single transaction record.
/// </summary>
public class Transaction : IEquatable<Transaction>
{
    /// <summary>
    /// Gets the unique identifier of the transaction.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the type of the transaction.
    /// </summary>
    public TransactionType Type { get; }

    /// <summary>
    /// Gets the monetary amount of the transaction.
    /// </summary>
    public Money Amount { get; }

    /// <summary>
    /// Gets the date and time when the transaction occurred.
    /// </summary>
    public DateTime TransactionDate { get; }

    /// <summary>
    /// Gets the external reference associated with the transaction.
    /// </summary>
    public string Reference { get; }

    /// <summary>
    /// Gets the optional description of the transaction.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the metadata associated with the transaction.
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Convenience alias for <see cref="TransactionDate"/>.
    /// Excluded from serialization so snapshot payloads remain unchanged.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime Timestamp => TransactionDate;

    /// <summary>
    /// Initializes a new transaction with a generated identifier and the current UTC date and time.
    /// </summary>
    /// <param name="type">The type of transaction.</param>
    /// <param name="amount">The monetary amount of the transaction.</param>
    /// <param name="reference">The external reference associated with the transaction.</param>
    /// <param name="description">The optional description of the transaction.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="amount"/> or <paramref name="reference"/> is <see langword="null"/>.
    /// </exception>
    public Transaction(TransactionType type, Money amount, string reference, string? description = null)
    {
        Id = Guid.NewGuid().ToString();
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Description = description;
        TransactionDate = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a transaction with an existing identifier and transaction date.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction.</param>
    /// <param name="type">The type of transaction.</param>
    /// <param name="amount">The monetary amount of the transaction.</param>
    /// <param name="transactionDate">The date and time when the transaction occurred.</param>
    /// <param name="reference">The external reference associated with the transaction.</param>
    /// <param name="description">The optional description of the transaction.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id"/>, <paramref name="amount"/>, or <paramref name="reference"/> is <see langword="null"/>.
    /// </exception>
    public Transaction(string id, TransactionType type, Money amount, DateTime transactionDate,
        string reference, string? description = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        TransactionDate = transactionDate;
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Description = description;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Determines whether this transaction is equal to another transaction.
    /// </summary>
    /// <param name="other">The transaction to compare with this transaction.</param>
    /// <returns>
    /// <see langword="true"/> when the transactions have the same identifier, type, and amount;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Transaction? other)
    {
        if (other is null)
            return false;

        return Id == other.Id && Type == other.Type && Amount == other.Amount;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Transaction);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Id, Type, Amount);

    /// <summary>
    /// Returns a string that represents the transaction.
    /// </summary>
    /// <returns>A string containing the transaction identifier, type, amount, date, and reference.</returns>
    public override string ToString()
        => $"Transaction {{ Id={Id}, Type={Type}, Amount={Amount}, Date={TransactionDate:yyyy-MM-dd HH:mm:ss}, Reference={Reference} }}";
}
