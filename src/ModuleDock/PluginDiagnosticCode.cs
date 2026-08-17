namespace ModuleDock;

/// <summary>
/// Stable diagnostic codes emitted while discovering and loading plugins.
/// </summary>
public enum PluginDiagnosticCode
{
    /// <summary>The configured plugin directory does not exist.</summary>
    MissingPluginDirectory = 1,

    /// <summary>A plugin directory does not contain <c>plugin.json</c>.</summary>
    MissingManifest = 2,

    /// <summary>The manifest is not valid JSON or could not be deserialized.</summary>
    MalformedManifest = 3,

    /// <summary>A required manifest field is missing or empty.</summary>
    MissingRequiredField = 4,

    /// <summary>The plugin identifier is missing or contains unsupported characters.</summary>
    InvalidPluginId = 5,

    /// <summary>A version value could not be parsed.</summary>
    InvalidVersion = 6,

    /// <summary>The plugin's contract version is newer than the host contract version.</summary>
    ContractMismatch = 7,

    /// <summary>The entry assembly file does not exist.</summary>
    MissingEntryAssembly = 8,

    /// <summary>The plugin's <c>.deps.json</c> file is missing.</summary>
    MissingDependencyMetadata = 9,

    /// <summary>The entry assembly path in the manifest is rooted.</summary>
    RootedEntryPath = 10,

    /// <summary>The entry assembly path contains <c>..</c> traversal.</summary>
    PathTraversal = 11,

    /// <summary>The resolved entry assembly is outside the plugin directory.</summary>
    EntryAssemblyOutsidePluginDirectory = 12,

    /// <summary>Two plugins declare the same identifier.</summary>
    DuplicatePluginId = 13,

    /// <summary>Two plugins declare the same capability.</summary>
    DuplicateCapability = 14,

    /// <summary>The entry assembly does not contain a concrete <see cref="IPluginModule"/>.</summary>
    NoPluginModule = 15,

    /// <summary>The entry assembly contains more than one concrete <see cref="IPluginModule"/>.</summary>
    MultiplePluginModules = 16,

    /// <summary>The plugin module could not be constructed.</summary>
    ModuleConstructionFailed = 17,

    /// <summary>The plugin threw while registering services.</summary>
    ServiceRegistrationFailed = 18,

    /// <summary>The supplied SHA-256 checksum does not match the entry assembly.</summary>
    ChecksumMismatch = 19,

    /// <summary>The supplied checksum is not a valid SHA-256 hex string.</summary>
    InvalidChecksum = 20,

    /// <summary>The entry assembly could not be loaded.</summary>
    PluginLoadFailed = 21,

    /// <summary>A plugin directory advertised the same capability more than once.</summary>
    DuplicateCapabilityInManifest = 22,

    /// <summary>A plugin directory or file could not be read.</summary>
    PluginDirectoryUnreadable = 23,

    /// <summary>The plugin's <c>.deps.json</c> file is present but not usable.</summary>
    MalformedDependencyMetadata = 24
}
