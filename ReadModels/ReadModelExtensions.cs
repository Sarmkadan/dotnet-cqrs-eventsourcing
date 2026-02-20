#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Options;

namespace DotNetCqrsEventSourcing.ReadModels;

/// <summary>
/// Fluent extension methods for registering the read-model projection infrastructure
/// in an <see cref="IServiceCollection"/>.
/// </summary>
/// <example>
/// <code>
/// services
///     .AddCqrsFramework()
///     .AddReadModelProjections(opts =>
///     {
///         opts.MaxRetryAttempts        = 5;
///         opts.MaxConcurrentProjectors = 8;
///         opts.EnableCheckpointing     = true;
///     })
///     .AddAccountProjections();
///
/// // After building the provider, activate the engine so it subscribes to the event bus:
/// var provider = services.BuildServiceProvider();
/// provider.UseReadModelProjections();
/// </code>
/// </example>
public static class ReadModelExtensions
{
    /// <summary>
    /// Registers the <see cref="ReadModelProjectionEngine"/> and its configuration.
    /// </summary>
    /// <remarks>
    /// This method must be called <em>after</em> <c>AddCqrsFramework()</c> because the engine
    /// depends on <c>IEventBus</c> and <c>IEventStore</c>, which are registered by that method.
    /// Call <see cref="AddAccountProjections"/> (or your own domain-specific projector registration)
    /// afterwards to wire up concrete <see cref="IReadModelProjectionRunner"/> instances.
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Optional delegate for overriding <see cref="ReadModelProjectionOptions"/> defaults.
    /// When omitted, the default values defined on the options class are used.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance for further chaining.</returns>
    public static IServiceCollection AddReadModelProjections(
        this IServiceCollection services,
        Action<ReadModelProjectionOptions>? configure = null)
    {
        var options = new ReadModelProjectionOptions();
        configure?.Invoke(options);

        services.AddSingleton(Options.Create(options));
        services.AddSingleton<ReadModelProjectionEngine>();

        return services;
    }

    /// <summary>
    /// Registers all services required to maintain the <see cref="AccountReadModel"/>
    /// materialized view: the in-memory store, the <see cref="AccountProjector"/>,
    /// the typed runner that couples them, the <see cref="IAccountReadModelQueryService"/>
    /// façade, and the <see cref="ProjectionDiagnosticsService"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for further chaining.</returns>
    public static IServiceCollection AddAccountProjections(this IServiceCollection services)
    {
        // Store — swap InMemoryReadModelStore for a database implementation in production.
        services.AddSingleton<IReadModelStore<AccountReadModel>,
            InMemoryReadModelStore<AccountReadModel>>();

        // Projector
        services.AddSingleton<IReadModelProjector<AccountReadModel>, AccountProjector>();

        // Runner — couples projector + store and exposes a non-generic handle for the engine.
        services.AddSingleton<IReadModelProjectionRunner>(sp =>
            new ReadModelProjectionRunner<AccountReadModel>(
                sp.GetRequiredService<IReadModelProjector<AccountReadModel>>(),
                sp.GetRequiredService<IReadModelStore<AccountReadModel>>()));

        // Query façade
        services.AddSingleton<IAccountReadModelQueryService, AccountReadModelQueryService>();

        // Diagnostics
        services.AddSingleton<ProjectionDiagnosticsService>();

        return services;
    }

    /// <summary>
    /// Resolves and activates the <see cref="ReadModelProjectionEngine"/> singleton,
    /// causing it to subscribe to the event bus.
    /// </summary>
    /// <remarks>
    /// Because the engine is registered as a singleton, the DI container will not activate it
    /// until the first call to <c>GetService&lt;ReadModelProjectionEngine&gt;()</c>.
    /// Calling this method eagerly after building the service provider ensures that the engine
    /// is subscribed before the first command is processed and no events are missed.
    /// </remarks>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>
    /// The activated <see cref="ReadModelProjectionEngine"/> instance,
    /// already subscribed to the event bus.
    /// </returns>
    public static ReadModelProjectionEngine UseReadModelProjections(
        this IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<ReadModelProjectionEngine>();
}
