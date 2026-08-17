using System.Collections.Frozen;

namespace ModuleDock;

/// <summary>
/// Immutable description of a plugin that loaded successfully.
/// </summary>
public sealed class PluginDescriptor
{
    /// <summary>
    /// Initialises a descriptor.
    /// </summary>
    /// <param name="id">Plugin identifier.</param>
    /// <param name="version">Plugin version.</param>
    /// <param name="contractVersion">Contract version declared by the plugin.</param>
    /// <param name="capabilities">Capabilities advertised by the plugin.</param>
    /// <param name="directory">Plugin directory.</param>
    /// <param name="entryAssembly">Path of the entry assembly, relative to the plugin directory.</param>
    public PluginDescriptor(
        string id,
        string version,
        string contractVersion,
        IReadOnlySet<string> capabilities,
        string directory,
        string entryAssembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryAssembly);

        Id = id;
        Version = version;
        ContractVersion = contractVersion;
        Capabilities = capabilities.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        Directory = directory;
        EntryAssembly = entryAssembly;
    }

    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the contract version declared by the plugin.
    /// </summary>
    public string ContractVersion { get; }

    /// <summary>
    /// Gets the capabilities advertised by the plugin.
    /// </summary>
    public IReadOnlySet<string> Capabilities { get; }

    /// <summary>
    /// Gets the plugin directory.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Gets the path of the entry assembly, relative to <see cref="Directory"/>.
    /// </summary>
    public string EntryAssembly { get; }
}
