using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using ModuleDock.Internal;

namespace ModuleDock;

/// <summary>
/// Registers ModuleDock on the .NET Generic Host before <c>Build()</c>.
/// </summary>
public static class ModuleDockHostBuilderExtensions
{
    /// <summary>
    /// Discovers, validates and registers startup plugins on the Generic Host.
    /// </summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">Configures plugin discovery. Relative plugin directories are resolved against the content root.</param>
    /// <returns>The same host builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginValidationException">Loading failed under <see cref="PluginFailurePolicy.FailFast"/>.</exception>
    /// <example>
    /// <code>
    /// builder.AddModuleDock(options =>
    /// {
    ///     options.PluginDirectory = Path.Combine(builder.Environment.ContentRootPath, "plugins");
    ///     options.ContractVersion = "1.0.0";
    ///     options.FailurePolicy = PluginFailurePolicy.FailFast;
    ///     options.ShareAssemblyContaining&lt;IHostContract&gt;();
    /// });
    /// </code>
    /// </example>
    /// <remarks>
    /// Call this before <c>builder.Build()</c>. Installing or upgrading a plugin requires
    /// replacing its directory and restarting the host. Runtime unloading is not supported
    /// once plugin types are registered in the root service provider.
    /// </remarks>
    [RequiresUnreferencedCode(TrimmingMessages.Discovery)]
    [RequiresDynamicCode(TrimmingMessages.Discovery)]
    public static TBuilder AddModuleDock<TBuilder>(
        this TBuilder builder,
        Action<ModuleDockOptions> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ModuleDockOptions();
        configure(options);

        if (!string.IsNullOrWhiteSpace(options.PluginDirectory)
            && !Path.IsPathRooted(options.PluginDirectory))
        {
            options.PluginDirectory = Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, options.PluginDirectory));
        }

        PluginComposer.AddModuleDock(builder.Services, options);
        return builder;
    }
}
