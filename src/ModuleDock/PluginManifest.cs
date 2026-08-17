using System.Text.Json.Serialization;

namespace ModuleDock;

/// <summary>
/// The validated contents of a plugin <c>plugin.json</c> manifest.
/// </summary>
public sealed class PluginManifest
{
    /// <summary>
    /// Initialises a manifest.
    /// </summary>
    /// <param name="id">Plugin identifier.</param>
    /// <param name="version">Plugin version.</param>
    /// <param name="entryAssembly">Entry assembly file name, relative to the plugin directory.</param>
    /// <param name="contractVersion">Contract version required by the plugin.</param>
    /// <param name="capabilities">Capabilities advertised by the plugin.</param>
    /// <param name="checksumSha256">Optional SHA-256 checksum of the entry assembly, as lowercase hex.</param>
    [JsonConstructor]
    public PluginManifest(
        string id,
        string version,
        string entryAssembly,
        string contractVersion,
        IReadOnlyList<string>? capabilities = null,
        string? checksumSha256 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryAssembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);

        Id = id;
        Version = version;
        EntryAssembly = entryAssembly;
        ContractVersion = contractVersion;
        Capabilities = capabilities ?? [];
        ChecksumSha256 = checksumSha256;
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
    /// Gets the entry assembly file name, relative to the plugin directory.
    /// </summary>
    public string EntryAssembly { get; }

    /// <summary>
    /// Gets the contract version required by the plugin.
    /// </summary>
    public string ContractVersion { get; }

    /// <summary>
    /// Gets the capabilities advertised by the plugin.
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Gets the optional SHA-256 checksum of the entry assembly.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? ChecksumSha256 { get; }
}
