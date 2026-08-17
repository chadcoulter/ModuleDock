using System.Globalization;
using System.Text.RegularExpressions;

namespace ModuleDock.Internal;

internal static class PluginIdValidator
{
    private static readonly Regex _validId = new(
        "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsValid(string? pluginId) =>
        !string.IsNullOrWhiteSpace(pluginId) && _validId.IsMatch(pluginId.Trim());
}

internal static class PluginVersion
{
    public static bool TryParse(string? value, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Version.TryParse(value.Trim(), out var parsed))
        {
            return false;
        }

        version = Normalize(parsed);
        return true;
    }

    public static Version Normalize(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);
        return new Version(
            version.Major,
            version.Minor,
            version.Build < 0 ? 0 : version.Build,
            version.Revision < 0 ? 0 : version.Revision);
    }

    public static int Compare(string left, string right)
    {
        if (!TryParse(left, out var leftVersion) || !TryParse(right, out var rightVersion))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Cannot compare '{0}' and '{1}' as versions.",
                    left,
                    right));
        }

        return leftVersion.CompareTo(rightVersion);
    }
}
