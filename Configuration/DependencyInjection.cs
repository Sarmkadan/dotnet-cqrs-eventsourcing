// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Application.Services;
using Data.Repositories;
using Domain.AggregateRoots;

/// <summary>
/// Dependency injection configuration for the CQRS framework.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register all services, repositories, and event handlers.
    /// </summary>
    public static IServiceCollection AddCqrsFramework(this IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IRepository<Account>, AccountRepository>();

        // Event handling
        services.AddSingleton<IEventStore, EventStore>();
        services.AddSingleton<IEventBus, EventBus>();

        // Projections and snapshots
        services.AddSingleton<IProjectionService, ProjectionService>();
        services.AddSingleton<ISnapshotService, SnapshotService>();

        // Application services
        services.AddSingleton<IAccountService, AccountService>();

        return services;
    }

    /// <summary>
    /// Configure event handlers and subscriptions.
    /// </summary>
    public static void ConfigureEventHandlers(this IServiceProvider serviceProvider)
    {
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var projectionService = serviceProvider.GetRequiredService<IProjectionService>();

        // Subscribe projection service to all domain events
        eventBus.Subscribe<Domain.Events.DomainEvent>(async (@event) =>
        {
            await projectionService.UpdateProjectionAsync(@event);
        });
    }
}
