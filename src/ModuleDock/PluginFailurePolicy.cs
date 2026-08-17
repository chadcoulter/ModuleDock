namespace ModuleDock;

/// <summary>
/// Policy that controls how ModuleDock reacts to invalid plugins during startup.
/// </summary>
public enum PluginFailurePolicy
{
    /// <summary>
    /// Throw <see cref="PluginValidationException"/> before the host is built if any plugin is invalid.
    /// </summary>
    FailFast = 0,

    /// <summary>
    /// Exclude invalid plugins from the catalogue while preserving diagnostics in <see cref="PluginLoadReport"/>.
    /// </summary>
    SkipInvalid = 1
}
