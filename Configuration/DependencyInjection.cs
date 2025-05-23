#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Configuration;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Application.Services;
using Data.Repositories;
using Application.Sagas;
using Domain.AggregateRoots;
using Infrastructure.Events;

/// <summary>
/// Dependency injection configuration for the CQRS framework.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register all services, repositories, and event handlers using configuration
    /// loaded from <c>appsettings.json</c> (when present) and environment variables.
    /// </summary>
    public static IServiceCollection AddCqrsFramework(this IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return services.AddCqrsFramework(configuration);
    }

    /// <summary>
    /// Register all services, repositories, and event handlers.
    /// </summary>
    public static IServiceCollection AddCqrsFramework(this IServiceCollection services, IConfiguration configuration)
    {
        // Options registration with validation
        services.AddOptions<DotnetCqrsEventsourcingOptions>()
            .Bind(configuration.GetSection(DotnetCqrsEventsourcingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Event type registry – scan the domain assembly so every [EventName(...)]-decorated
        // event is discoverable by the EventStore deserializer without relying on Type.GetType().
        services.AddSingleton(sp =>
        {
            var registry = new EventTypeRegistry(null);
            registry.ScanAssembly(typeof(Domain.Events.DomainEvent).Assembly);
            return registry;
        });

        // Repositories
        services.AddSingleton<IEventRepository, InMemoryEventRepository>();
        services.AddSingleton<IRepository<Account>, AccountRepository>();

        // Event handling
        services.AddSingleton<IEventStore, EventStore>();
        services.AddSingleton<IEventBus, EventBus>();

        // Projections and snapshots
        services.AddSingleton<IProjectionService, ProjectionService>();
        services.AddSingleton<ISnapshotService, SnapshotService>();

        // Event store compaction
        services.AddSingleton<IEventStoreCompactionService, EventStoreCompactionService>();

        // Saga orchestration
        services.AddSingleton<SagaOrchestrator>();

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
