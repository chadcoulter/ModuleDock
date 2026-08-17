namespace ModuleDock;

/// <summary>
/// Runtime information supplied to a plugin module while it registers services.
/// </summary>
/// <remarks>
/// The context is created by the host from the plugin's <c>plugin.json</c> manifest.
/// Plugin authors should treat it as descriptive metadata, not as a service locator.
/// </remarks>
public sealed class PluginContext
{
    /// <summary>
    /// Initialises a new plugin context.
    /// </summary>
    /// <param name="pluginId">The plugin identifier from the manifest.</param>
    /// <param name="pluginVersion">The plugin version from the manifest.</param>
    /// <param name="contractVersion">The contract version declared by the plugin.</param>
    /// <param name="capabilities">Capabilities advertised by the plugin. Comparisons are case-insensitive.</param>
    /// <param name="pluginDirectory">The directory that contains the plugin's manifest and entry assembly.</param>
    public PluginContext(
        string pluginId,
        string pluginVersion,
        string contractVersion,
        IReadOnlySet<string> capabilities,
        string pluginDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        PluginId = pluginId;
        PluginVersion = pluginVersion;
        ContractVersion = contractVersion;
        Capabilities = capabilities;
        PluginDirectory = pluginDirectory;
    }

    /// <summary>
    /// Gets the plugin identifier from the manifest.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the plugin version from the manifest.
    /// </summary>
    public string PluginVersion { get; }

    /// <summary>
    /// Gets the contract version declared by the plugin.
    /// </summary>
    public string ContractVersion { get; }

    /// <summary>
    /// Gets the capabilities advertised by the plugin.
    /// </summary>
    public IReadOnlySet<string> Capabilities { get; }

    /// <summary>
    /// Gets the directory that contains the plugin's manifest and entry assembly.
    /// </summary>
    public string PluginDirectory { get; }
}
