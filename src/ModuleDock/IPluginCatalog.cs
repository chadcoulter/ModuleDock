namespace ModuleDock;

/// <summary>
/// Immutable catalogue of plugins that loaded successfully at startup.
/// </summary>
/// <remarks>
/// The catalogue is fixed after <c>AddModuleDock</c> returns. Installing or upgrading a
/// plugin requires replacing its directory and restarting the host.
/// </remarks>
public interface IPluginCatalog
{
    /// <summary>
    /// Gets the plugins that loaded successfully, in deterministic directory order.
    /// </summary>
    public IReadOnlyList<PluginDescriptor> Plugins { get; }

    /// <summary>
    /// Gets the full load report, including diagnostics for skipped plugins.
    /// </summary>
    public PluginLoadReport LoadReport { get; }

    /// <summary>
    /// Finds a loaded plugin by identifier.
    /// </summary>
    /// <param name="pluginId">Plugin identifier. Comparison is case-insensitive.</param>
    /// <returns>The descriptor, or <see langword="null"/> if the plugin was not loaded.</returns>
    public PluginDescriptor? FindById(string pluginId);

    /// <summary>
    /// Finds a loaded plugin by capability.
    /// </summary>
    /// <param name="capability">Capability name. Comparison is case-insensitive.</param>
    /// <returns>The descriptor, or <see langword="null"/> if no loaded plugin advertises the capability.</returns>
    public PluginDescriptor? FindByCapability(string capability);
}
