#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Wrapper for domain events with infrastructure metadata for event store persistence.
/// </summary>
public sealed class EventEnvelope
{
    /// <summary>
    /// Gets or sets the unique identifier of the event envelope.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the aggregate that produced the event.
    /// </summary>
    public string AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the type of the aggregate that produced the event.
    /// </summary>
    public string AggregateType { get; set; }

    /// <summary>
    /// Gets or sets the aggregate version associated with the event.
    /// </summary>
    public long AggregateVersion { get; set; }

    /// <summary>
    /// Gets or sets the persisted type name of the event.
    /// </summary>
    public string EventType { get; set; }

    /// <summary>
    /// Gets or sets the serialized event payload.
    /// </summary>
    public string EventData { get; set; }

    /// <summary>
    /// Gets or sets the metadata associated with the event.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the checksum used to verify the integrity of the event data.
    /// </summary>
    public string? ChecksumHash { get; set; }

    /// <summary>
    /// Optional partition key for multi-tenant event stream isolation.
    /// When set, this value (e.g. a tenant ID) logically or physically separates
    /// events so that per-tenant replay, snapshot, and archival operations can be
    /// performed without cross-tenant data access.
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class with a unique identifier
    /// and default values.
    /// </summary>
    public EventEnvelope()
    {
        Id = Guid.NewGuid().ToString();
        AggregateId = string.Empty;
        AggregateType = string.Empty;
        EventType = string.Empty;
        EventData = string.Empty;
        Metadata = new Dictionary<string, string>();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEnvelope"/> class from a domain event and
    /// its serialized payload.
    /// </summary>
    /// <param name="domainEvent">The domain event to wrap.</param>
    /// <param name="serializedData">The serialized event payload.</param>
    public EventEnvelope(DomainEvent domainEvent, string serializedData)
        : this()
    {
        AggregateId = domainEvent.AggregateId;
        AggregateType = domainEvent.AggregateType;
        AggregateVersion = domainEvent.AggregateVersion;
        EventType = domainEvent.GetEventType();
        EventData = serializedData;
        PartitionKey = domainEvent.TenantId;

        domainEvent.PopulateMetadata();
        foreach (var kvp in domainEvent.Metadata)
        {
            Metadata[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
        }

        CreatedAt = domainEvent.OccurredAt;
    }

    /// <summary>
    /// Computes and stores a checksum for the aggregate and event data in this envelope.
    /// </summary>
    public void ComputeChecksum()
    {
        var checksumData = $"{AggregateId}:{AggregateVersion}:{EventType}:{EventData}";
        ChecksumHash = ComputeSha256Hash(checksumData);
    }

    /// <summary>
    /// Verifies that the stored checksum matches the aggregate and event data in this envelope.
    /// </summary>
    /// <returns><see langword="true"/> when the checksum is present and valid; otherwise, <see langword="false"/>.</returns>
    public bool VerifyChecksum()
    {
        if (string.IsNullOrEmpty(ChecksumHash))
            return false;

        ComputeChecksum();
        return ChecksumHash == ComputeSha256Hash($"{AggregateId}:{AggregateVersion}:{EventType}:{EventData}");
    }

    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    /// <summary>
    /// Returns a string that represents the event envelope.
    /// </summary>
    /// <returns>A string containing the envelope identifier, aggregate identifier, version, and event type.</returns>
    public override string ToString()
        => $"EventEnvelope {{ Id={Id}, AggregateId={AggregateId}, Version={AggregateVersion}, EventType={EventType} }}";
}
