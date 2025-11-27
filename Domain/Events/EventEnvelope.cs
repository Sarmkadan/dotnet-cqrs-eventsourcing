// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Wrapper for domain events with infrastructure metadata for event store persistence.
/// </summary>
public class EventEnvelope
{
    public string Id { get; set; }
    public string AggregateId { get; set; }
    public string AggregateType { get; set; }
    public long AggregateVersion { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ChecksumHash { get; set; }

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

    public EventEnvelope(DomainEvent domainEvent, string serializedData)
        : this()
    {
        AggregateId = domainEvent.AggregateId;
        AggregateType = domainEvent.AggregateType;
        AggregateVersion = domainEvent.AggregateVersion;
        EventType = domainEvent.GetEventType();
        EventData = serializedData;

        domainEvent.PopulateMetadata();
        foreach (var kvp in domainEvent.Metadata)
        {
            Metadata[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
        }

        CreatedAt = domainEvent.OccurredAt;
    }

    public void ComputeChecksum()
    {
        var checksumData = $"{AggregateId}:{AggregateVersion}:{EventType}:{EventData}";
        ChecksumHash = ComputeSha256Hash(checksumData);
    }

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

    public override string ToString()
        => $"EventEnvelope {{ Id={Id}, AggregateId={AggregateId}, Version={AggregateVersion}, EventType={EventType} }}";
}
