// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Application.Services;

using Domain.Events;
using Shared.Results;

/// <summary>
/// Projection service interface for building and maintaining read models from events.
/// </summary>
public interface IProjectionService
{
    Task<Result> UpdateProjectionAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    Task<Result> RebuildProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result> RebuildAllProjectionsAsync(CancellationToken cancellationToken = default);
    Task<Result<Dictionary<string, object>>> GetProjectionAsync(string aggregateId, CancellationToken cancellationToken = default);
    Task<Result<List<Dictionary<string, object>>>> GetAllProjectionsAsync(CancellationToken cancellationToken = default);
}
