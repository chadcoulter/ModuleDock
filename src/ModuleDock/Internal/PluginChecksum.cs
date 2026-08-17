using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ModuleDock;

namespace ModuleDock.Internal;

internal static class PluginChecksum
{
    private static readonly Regex _hexSha256 = new(
        "^(?:sha256:)?[0-9a-fA-F]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsWellFormed(string checksum) => _hexSha256.IsMatch(checksum.Trim());

    public static string ComputeSha256Hex(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    public static bool Matches(string filePath, string checksum)
    {
        var expected = Normalize(checksum);
        var actual = ComputeSha256Hex(filePath);
        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string checksum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checksum);
        var trimmed = checksum.Trim();
        if (trimmed.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["sha256:".Length..];
        }

        return trimmed.ToUpperInvariant();
    }

    public static PluginDiagnostic? Validate(string filePath, string? checksum, string? pluginId, string pluginPath)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return null;
        }

        if (!IsWellFormed(checksum))
        {
            return new PluginDiagnostic(
                PluginDiagnosticCode.InvalidChecksum,
                "The manifest checksum must be a 64-character SHA-256 hex string, optionally prefixed with 'sha256:'.",
                pluginId,
                pluginPath);
        }

        if (!Matches(filePath, checksum))
        {
            return new PluginDiagnostic(
                PluginDiagnosticCode.ChecksumMismatch,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The SHA-256 checksum of '{0}' does not match the value in the manifest.",
                    Path.GetFileName(filePath)),
                pluginId,
                pluginPath);
        }

        return null;
    }
}
