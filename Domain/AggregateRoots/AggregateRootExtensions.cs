#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.AggregateRoots;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension methods for <see cref="AggregateRoot"/> providing common operations on aggregate roots.
/// </summary>
public static class AggregateRootExtensions
{
    /// <summary>
    /// Determines whether this aggregate has any uncommitted events.
    /// </summary>
    /// <param name="aggregate">The aggregate root instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    /// <returns><see langword="true"/> if there are uncommitted events; otherwise, <see langword="false"/>.</returns>
    public static bool HasUncommittedEvents(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.GetUncommittedEvents().Count > 0;
    }

    /// <summary>
    /// Gets the number of uncommitted events for this aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate root instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    /// <returns>The count of uncommitted events.</returns>
    public static int UncommittedEventsCount(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.GetUncommittedEvents().Count;
    }

    /// <summary>
    /// Determines whether this aggregate has been modified since it was created.
    /// </summary>
    /// <param name="aggregate">The aggregate root instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    /// <returns><see langword="true"/> if the aggregate has been modified (Version > 0); otherwise, <see langword="false"/>.</returns>
    public static bool IsModified(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return aggregate.Version > 0;
    }

    /// <summary>
    /// Gets the age of the aggregate in UTC.
    /// </summary>
    /// <param name="aggregate">The aggregate root instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aggregate"/> is <see langword="null"/>.</exception>
    /// <returns>A <see cref="TimeSpan"/> representing how long the aggregate has existed.</returns>
    public static TimeSpan GetAge(this AggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return DateTime.UtcNow - aggregate.CreatedAt;
    }
}