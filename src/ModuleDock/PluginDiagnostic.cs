namespace ModuleDock;

/// <summary>
/// A structured diagnostic produced while discovering or loading a plugin.
/// </summary>
public sealed class PluginDiagnostic
{
    /// <summary>
    /// Initialises a diagnostic.
    /// </summary>
    /// <param name="code">Stable diagnostic code.</param>
    /// <param name="message">Human-readable explanation that does not include secrets.</param>
    /// <param name="pluginId">Plugin identifier when it is known.</param>
    /// <param name="pluginPath">Plugin directory or manifest path.</param>
    /// <param name="capability">Capability involved in a collision, when applicable.</param>
    public PluginDiagnostic(
        PluginDiagnosticCode code,
        string message,
        string? pluginId = null,
        string? pluginPath = null,
        string? capability = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        PluginId = pluginId;
        PluginPath = pluginPath;
        Capability = capability;
    }

    /// <summary>
    /// Gets the stable diagnostic code.
    /// </summary>
    public PluginDiagnosticCode Code { get; }

    /// <summary>
    /// Gets a readable explanation of the problem.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the plugin identifier when it is known.
    /// </summary>
    public string? PluginId { get; }

    /// <summary>
    /// Gets the plugin directory or manifest path.
    /// </summary>
    public string? PluginPath { get; }

    /// <summary>
    /// Gets the capability involved in a collision, when applicable.
    /// </summary>
    public string? Capability { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var prefix = PluginId is null ? PluginPath ?? "plugin" : PluginId;
        return $"[{Code}] {prefix}: {Message}";
    }
}
