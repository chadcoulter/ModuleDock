using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ModuleDock.Internal;

namespace ModuleDock;

/// <summary>
/// Registers ModuleDock plugins into an <see cref="IServiceCollection"/> before the provider is built.
/// </summary>
public static class ModuleDockServiceCollectionExtensions
{
    /// <summary>
    /// Discovers plugins, validates manifests, loads each plugin into its own
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, and registers plugin
    /// services into <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configure">Configures plugin discovery.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginValidationException">Loading failed under <see cref="PluginFailurePolicy.FailFast"/>.</exception>
    /// <example>
    /// <code>
    /// services.AddModuleDock(options =>
    /// {
    ///     options.PluginDirectory = pluginRoot;
    ///     options.ContractVersion = "1.0.0";
    ///     options.ShareAssemblyContaining&lt;IHostContract&gt;();
    /// });
    /// </code>
    /// </example>
    /// <remarks>
    /// Trimming and Native AOT are not supported. Plugin types are discovered by reflection
    /// and loaded at runtime. <see cref="System.Runtime.Loader.AssemblyLoadContext"/> provides
    /// dependency isolation, not a security boundary.
    /// </remarks>
    [RequiresUnreferencedCode(TrimmingMessages.Discovery)]
    [RequiresDynamicCode(TrimmingMessages.Discovery)]
    public static IServiceCollection AddModuleDock(
        this IServiceCollection services,
        Action<ModuleDockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ModuleDockOptions();
        configure(options);
        PluginComposer.AddModuleDock(services, options);
        return services;
    }
}
