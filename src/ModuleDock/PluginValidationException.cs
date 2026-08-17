using System.Collections.ObjectModel;
using System.Text;

namespace ModuleDock;

/// <summary>
/// Thrown when plugin discovery or loading fails under <see cref="PluginFailurePolicy.FailFast"/>.
/// </summary>
public sealed class PluginValidationException : Exception
{
    /// <summary>
    /// Initialises a validation exception.
    /// </summary>
    /// <param name="message">Summary of the failure.</param>
    /// <param name="diagnostics">Diagnostics collected during the load.</param>
    /// <param name="report">The load report, when available.</param>
    /// <param name="innerException">The original exception, when a plugin threw.</param>
    public PluginValidationException(
        string message,
        IReadOnlyList<PluginDiagnostic> diagnostics,
        PluginLoadReport? report = null,
        Exception? innerException = null)
        : base(BuildMessage(message, diagnostics), innerException)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = new ReadOnlyCollection<PluginDiagnostic>([.. diagnostics]);
        Report = report;
    }

    /// <summary>
    /// Gets the diagnostics that caused the failure.
    /// </summary>
    public IReadOnlyList<PluginDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the load report, when one was produced.
    /// </summary>
    public PluginLoadReport? Report { get; }

    private static string BuildMessage(string message, IReadOnlyList<PluginDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (diagnostics.Count == 0)
        {
            return message;
        }

        var builder = new StringBuilder(message);
        builder.Append(" Diagnostics:");
        foreach (var diagnostic in diagnostics)
        {
            builder.AppendLine();
            builder.Append(" - ");
            builder.Append(diagnostic);
        }

        return builder.ToString();
    }
}
