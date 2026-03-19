#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Events;

/// <summary>
/// Base class for all domain events in the event sourcing system.
/// </summary>
public abstract class DomainEvent
{
    public string EventId { get; set; }
    public string AggregateId { get; set; }
    public string AggregateType { get; set; }
    public long AggregateVersion { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? UserId { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Optional tenant identifier used for multi-tenant event stream partitioning.
    /// When set, this value is propagated to <see cref="EventEnvelope.PartitionKey"/>
    /// so that per-tenant replay, snapshot, and archival can be performed in isolation.
    /// Leave <see langword="null"/> for single-tenant deployments (backward-compatible).
    /// </summary>
    public string? TenantId { get; set; }

    public Dictionary<string, object> Metadata { get; set; }

    protected DomainEvent()
    {
        EventId = Guid.NewGuid().ToString();
        AggregateId = string.Empty;
        AggregateType = string.Empty;
        AggregateVersion = 0;
        OccurredAt = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    protected DomainEvent(string aggregateId, string aggregateType, long aggregateVersion)
        : this()
    {
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        AggregateVersion = aggregateVersion;
    }

    public abstract string GetEventType();

    public virtual void PopulateMetadata()
    {
        Metadata[nameof(AggregateId)] = AggregateId;
        Metadata[nameof(AggregateType)] = AggregateType;
        Metadata[nameof(AggregateVersion)] = AggregateVersion;
        Metadata[nameof(OccurredAt)] = OccurredAt;

        if (!string.IsNullOrEmpty(UserId))
            Metadata[nameof(UserId)] = UserId;

        if (!string.IsNullOrEmpty(CorrelationId))
            Metadata[nameof(CorrelationId)] = CorrelationId;

        if (!string.IsNullOrEmpty(TenantId))
            Metadata[nameof(TenantId)] = TenantId;
    }

    public override string ToString()
        => $"{GetType().Name} {{ AggregateId={AggregateId}, Version={AggregateVersion}, OccurredAt={OccurredAt} }}";
}
