// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Infrastructure.Caching;
using DotNetCqrsEventSourcing.Infrastructure.Events;
using DotNetCqrsEventSourcing.Infrastructure.Formatters;
using DotNetCqrsEventSourcing.Infrastructure.Integration;
using DotNetCqrsEventSourcing.Infrastructure.Observability;
using DotNetCqrsEventSourcing.Infrastructure.Workers;
using DotNetCqrsEventSourcing.Infrastructure.Idempotency;

namespace DotNetCqrsEventSourcing.Infrastructure.Configuration;

/// <summary>
/// Centralized infrastructure configuration for dependency injection.
/// Registers all middleware, services, and utilities needed for the CQRS framework.
/// Split into separate methods for modularity - enable/disable features as needed.
/// </summary>
public static class InfrastructureConfiguration
{
    /// <summary>
    /// Registers all infrastructure services and middleware.
    /// Call this in Program.cs during service configuration.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register all infrastructure services
        services
            .AddLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            })
            .AddCaching()
            .AddEventServices()
            .AddFormatters()
            .AddIntegration(configuration)
            .AddWorkers()
            .AddObservability()
            .AddIdempotency();

        return services;
    }

    /// <summary>
    /// Configures the ASP.NET request pipeline with infrastructure middleware.
    /// Call this in Program.cs during app configuration.
    /// </summary>
    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Order matters - place global error handling first to catch exceptions from all middleware
        return app
            .UseGlobalErrorHandling()
            .UseRequestContext()
            .UseRequestLogging()
            .UseRateLimiting()
            .UseIdempotency();
    }

    /// <summary>
    /// Registers caching services (in-memory and distributed).
    /// </summary>
    private static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        return services;
    }

    /// <summary>
    /// Registers event publishing and dispatching services.
    /// These are core to event sourcing pattern.
    /// </summary>
    private static IServiceCollection AddEventServices(this IServiceCollection services)
    {
        services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers formatters for different output formats (JSON, CSV, etc.).
    /// </summary>
    private static IServiceCollection AddFormatters(this IServiceCollection services)
    {
        services.AddJsonFormatter();
        services.AddCsvFormatter();
        return services;
    }

    /// <summary>
    /// Registers external integration services (HTTP clients, webhooks, etc.).
    /// </summary>
    private static IServiceCollection AddIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStandardHttpClients();
        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers background workers for snapshot creation, projection rebuilding, etc.
    /// </summary>
    private static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        services.AddSingleton<ISnapshotWorker, SnapshotWorker>();
        services.AddSingleton<IProjectionWorker, ProjectionWorker>();

        // Register as hosted services so they start automatically
        services.AddHostedService(sp => sp.GetRequiredService<ISnapshotWorker>());
        services.AddHostedService(sp => sp.GetRequiredService<IProjectionWorker>());

        return services;
    }

    /// <summary>
    /// Registers observability and monitoring services.
    /// </summary>
    private static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddHealthChecks()
            .AddCheck<PerformanceHealthCheck>("performance");
        return services;
    }

    /// <summary>
    /// Registers idempotency key handling.
    /// </summary>
    private static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        services.AddIdempotency();
        return services;
    }
}

/// <summary>
/// Application-wide configuration builder for fluent setup.
/// Example:
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Services
///     .AddCqrs()
///     .AddInfrastructure(builder.Configuration)
/// </summary>
public static class ApplicationConfigurationExtensions
{
    /// <summary>
    /// Configures all CQRS framework infrastructure in one call.
    /// </summary>
    public static WebApplicationBuilder ConfigureCqrsFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Add infrastructure services
        builder.Services.AddInfrastructure(builder.Configuration);

        // Configure middleware pipeline
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Use infrastructure middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseInfrastructure();
        app.MapControllers();
        app.MapHealthChecks("/health");

        return builder;
    }
}
