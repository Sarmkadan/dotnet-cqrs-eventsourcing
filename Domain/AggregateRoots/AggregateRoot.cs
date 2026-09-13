#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.AggregateRoots;

using Events;

/// <summary>
/// Base class for all aggregate roots in the domain. Manages event sourcing and state reconstruction.
/// </summary>
public abstract class AggregateRoot
{
    /// <summary>
    /// Gets the aggregate ID.
    /// </summary>
    public string Id { get; protected set; }
    /// <summary>
    /// Gets the current aggregate version.
    /// </summary>
    public long Version { get; protected set; }
    /// <summary>
    /// Gets the creation time.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }
    /// <summary>
    /// Gets the last update time.
    /// </summary>
    public DateTime UpdatedAt { get; protected set; }

    private readonly List<DomainEvent> _uncommittedEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    protected AggregateRoot()
    {
        Id = Guid.NewGuid().ToString();
        Version = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class with a specified ID.
    /// </summary>
    /// <param name="id">The aggregate ID.</param>
    protected AggregateRoot(string id)
        : this()
    {
        Id = id;
    }

    /// <summary>
    /// Optional tenant identifier.  When set, every event raised by this aggregate
    /// will carry this value as its <c>TenantId</c>, which is propagated to
    /// <see cref="EventEnvelope.PartitionKey"/> for per-tenant stream isolation.
    /// Leave <see langword="null"/> (the default) for single-tenant deployments.
    /// </summary>
    public string? TenantId { get; protected set; }

    /// <summary>
    /// Gets the domain events that have been raised since the aggregate was last committed.
    /// </summary>
    /// <returns>A read-only list of the aggregate's uncommitted domain events.</returns>
    public IReadOnlyList<DomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// Clears the domain events that have been recorded as uncommitted.
    /// </summary>
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    /// <summary>
    /// Reconstructs the aggregate state by replaying its event history.
    /// </summary>
    /// <param name="events">The domain events to replay in sequence.</param>
    public void LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        foreach (var @event in events)
        {
            ApplyEvent(@event, isFromHistory: true);
            Version = @event.AggregateVersion;
        }
    }

    /// <summary>
    /// Records a new domain event on the aggregate: stamps it with the aggregate
    /// identity, type, next version, occurrence time, and tenant, then applies it
    /// to state and adds it to the uncommitted event list.
    /// </summary>
    /// <param name="event">The domain event to raise.</param>
    protected void RaiseEvent(DomainEvent @event)
    {
        @event.AggregateId = Id;
        @event.AggregateType = GetType().Name;
        @event.AggregateVersion = Version + 1;
        @event.OccurredAt = DateTime.UtcNow;
        @event.TenantId = TenantId;

        ApplyEvent(@event, isFromHistory: false);

        Version++;
        _uncommittedEvents.Add(@event);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies a domain event to the aggregate's state. Must be overridden in derived
    /// classes to update properties based on the event data. Called both when loading
    /// from history (isFromHistory = true) and when raising new events (isFromHistory = false).
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    /// <param name="isFromHistory">True when replaying events from the event store; false for newly raised events.</param>
    protected abstract void ApplyEvent(DomainEvent @event, bool isFromHistory);

    /// <summary>
    /// Returns a string that represents the aggregate root.
    /// </summary>
    /// <returns>A string containing the aggregate type, ID, version, and creation time.</returns>
    public override string ToString()
        => $"{GetType().Name} {{ Id={Id}, Version={Version}, CreatedAt={CreatedAt} }}";
}
