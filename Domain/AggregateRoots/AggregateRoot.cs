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
    public string Id { get; protected set; }
    public long Version { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    private readonly List<DomainEvent> _uncommittedEvents = new();

    protected AggregateRoot()
    {
        Id = Guid.NewGuid().ToString();
        Version = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    protected AggregateRoot(string id)
        : this()
    {
        Id = id;
    }

    // Retrieve all uncommitted events since last commit
    public IReadOnlyList<DomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    // Clear uncommitted events after they've been persisted
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    // Load state from event history (replay)
    public void LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        foreach (var @event in events)
        {
            ApplyEvent(@event, isFromHistory: true);
            Version = @event.AggregateVersion;
        }
    }

    // Increment version and apply event
    protected void RaiseEvent(DomainEvent @event)
    {
        @event.AggregateId = Id;
        @event.AggregateType = GetType().Name;
        @event.AggregateVersion = Version + 1;
        @event.OccurredAt = DateTime.UtcNow;

        ApplyEvent(@event, isFromHistory: false);

        Version++;
        _uncommittedEvents.Add(@event);
        UpdatedAt = DateTime.UtcNow;
    }

    // Apply event to state - override in derived classes
    protected abstract void ApplyEvent(DomainEvent @event, bool isFromHistory);

    public override string ToString()
        => $"{GetType().Name} {{ Id={Id}, Version={Version}, CreatedAt={CreatedAt} }}";
}
