namespace ModuleDock.Internal;

internal static class PluginPathValidator
{
    public static bool IsInsideDirectory(string candidatePath, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var fullDirectory = AppendSeparator(Path.GetFullPath(directory));
        var fullCandidate = Path.GetFullPath(candidatePath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return fullCandidate.StartsWith(fullDirectory, comparison)
               || string.Equals(
                   fullCandidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                   fullDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                   comparison);
    }

    public static bool ContainsTraversal(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        if (relativePath == ".." || relativePath == ".")
        {
            return relativePath == "..";
        }

        var normalized = relativePath.Replace('\\', '/');
        return normalized.StartsWith("../", StringComparison.Ordinal)
               || normalized.Contains("/../", StringComparison.Ordinal)
               || normalized.EndsWith("/..", StringComparison.Ordinal);
    }

    private static string AppendSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
        || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
