using System.Reflection;

namespace ModuleDock;

/// <summary>
/// Configuration for discovering and loading startup plugins.
/// </summary>
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
public sealed class ModuleDockOptions
{
    private readonly List<Assembly> _sharedAssemblies = [];

    /// <summary>
    /// Gets or sets the directory that contains one subdirectory per plugin.
    /// </summary>
    /// <remarks>
    /// Relative paths passed to <c>AddModuleDock</c> on an <c>IHostApplicationBuilder</c>
    /// are resolved against the host content root.
    /// </remarks>
    public string PluginDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contract version provided by the host.
    /// </summary>
    /// <remarks>
    /// A plugin loads when its manifest <c>contractVersion</c> is less than or equal to
    /// this value. Use the same versioning scheme as the application contract assembly.
    /// </remarks>
    public string ContractVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how invalid plugins are handled. The default is
    /// <see cref="PluginFailurePolicy.FailFast"/>.
    /// </summary>
    public PluginFailurePolicy FailurePolicy { get; set; } = PluginFailurePolicy.FailFast;

    /// <summary>
    /// Gets or sets whether each plugin must ship a <c>.deps.json</c> file next to its
    /// entry assembly. The default is <see langword="true"/>.
    /// </summary>
    public bool RequireDependencyMetadata { get; set; } = true;

    /// <summary>
    /// Shares the assembly that contains <typeparamref name="T"/> with every plugin load
    /// context so host and plugin use the same type identity.
    /// </summary>
    /// <typeparam name="T">A type from a host or contract assembly that plugins must share.</typeparam>
    /// <returns>The same options instance.</returns>
    /// <remarks>
    /// Always share the application contract assembly. Do not share private plugin
    /// dependencies; those should resolve inside each plugin's load context.
    /// <see cref="IPluginModule"/>'s assembly is shared automatically.
    /// </remarks>
    public ModuleDockOptions ShareAssemblyContaining<T>()
    {
        var assembly = typeof(T).Assembly;
        if (!_sharedAssemblies.Contains(assembly))
        {
            _sharedAssemblies.Add(assembly);
        }

        return this;
    }

    internal IReadOnlyList<Assembly> SharedAssemblies => _sharedAssemblies;
}
