using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class ProjectionEngine
{
    private readonly ILogger<ProjectionEngine> _logger;
    private readonly Dictionary<string, ProjectionState> _projections = new();

    public ProjectionEngine(ILogger<ProjectionEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(string projectionName, Func<string, Task> processEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ArgumentNullException.ThrowIfNull(processEvent);

        if (_projections.TryGetValue(projectionName, out var projectionState))
        {
            await RunProjectionAsync(projectionState, processEvent, cancellationToken);
        }
        else
        {
            projectionState = new ProjectionState(projectionName);
            _projections[projectionName] = projectionState;
            await RunProjectionAsync(projectionState, processEvent, cancellationToken);
        }
    }

    private async Task RunProjectionAsync(ProjectionState projectionState, Func<string, Task> processEvent, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var @event = await GetNextEventAsync(projectionState, cancellationToken);
                if (@event is null)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                await processEvent(@event);
                projectionState.Checkpoint = @event;
                projectionState.ConsecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event in projection {ProjectionName}", projectionState.Name);
                projectionState.ConsecutiveFailures++;

                if (projectionState.ConsecutiveFailures >= 5)
                {
                    _logger.LogInformation("Circuit breaker triggered for projection {ProjectionName}. Pausing projection.", projectionState.Name);
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    projectionState.ConsecutiveFailures = 0;
                }
            }
        }
    }

    private async Task<string?> GetNextEventAsync(ProjectionState projectionState, CancellationToken cancellationToken)
    {
        // Implement logic to get the next event for the projection
        // For demonstration purposes, assume this method returns a string representing the event
        return await Task.FromResult("Event");
    }

    private sealed class ProjectionState
    {
        public string Name { get; }
        public string? Checkpoint { get; set; }
        public int ConsecutiveFailures { get; set; }

        public ProjectionState(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
