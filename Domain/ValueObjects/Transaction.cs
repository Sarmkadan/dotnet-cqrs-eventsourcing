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
    public string Id { get; }
    public TransactionType Type { get; }
    public Money Amount { get; }
    public DateTime TransactionDate { get; }
    public string Reference { get; }
    public string? Description { get; }
    public Dictionary<string, object> Metadata { get; }

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

    public bool Equals(Transaction? other)
    {
        if (other is null)
            return false;

        return Id == other.Id && Type == other.Type && Amount == other.Amount;
    }

    public override bool Equals(object? obj) => Equals(obj as Transaction);

    public override int GetHashCode() => HashCode.Combine(Id, Type, Amount);

    public override string ToString()
        => $"Transaction {{ Id={Id}, Type={Type}, Amount={Amount}, Date={TransactionDate:yyyy-MM-dd HH:mm:ss}, Reference={Reference} }}";
}
