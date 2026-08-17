using System.Collections.ObjectModel;

namespace ModuleDock;

/// <summary>
/// Immutable report of a plugin load attempt.
/// </summary>
public sealed class PluginLoadReport
{
    /// <summary>
    /// Initialises a load report.
    /// </summary>
    /// <param name="pluginDirectory">The configured plugin root directory.</param>
    /// <param name="failurePolicy">The failure policy used for this load.</param>
    /// <param name="hostContractVersion">The contract version advertised by the host.</param>
    /// <param name="loaded">Plugins that loaded and registered successfully.</param>
    /// <param name="diagnostics">Diagnostics collected during the load, including skipped plugins.</param>
    public PluginLoadReport(
        string pluginDirectory,
        PluginFailurePolicy failurePolicy,
        string hostContractVersion,
        IReadOnlyList<PluginDescriptor> loaded,
        IReadOnlyList<PluginDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostContractVersion);
        ArgumentNullException.ThrowIfNull(loaded);
        ArgumentNullException.ThrowIfNull(diagnostics);

        PluginDirectory = pluginDirectory;
        FailurePolicy = failurePolicy;
        HostContractVersion = hostContractVersion;
        Loaded = new ReadOnlyCollection<PluginDescriptor>([.. loaded]);
        Diagnostics = new ReadOnlyCollection<PluginDiagnostic>([.. diagnostics]);
    }

    /// <summary>
    /// Gets the configured plugin root directory.
    /// </summary>
    public string PluginDirectory { get; }

    /// <summary>
    /// Gets the failure policy used for this load.
    /// </summary>
    public PluginFailurePolicy FailurePolicy { get; }

    /// <summary>
    /// Gets the contract version advertised by the host.
    /// </summary>
    public string HostContractVersion { get; }

    /// <summary>
    /// Gets plugins that loaded and registered successfully.
    /// </summary>
    public IReadOnlyList<PluginDescriptor> Loaded { get; }

    /// <summary>
    /// Gets diagnostics collected during the load.
    /// </summary>
    public IReadOnlyList<PluginDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether any diagnostics were recorded.
    /// </summary>
    public bool HasDiagnostics => Diagnostics.Count > 0;
}
