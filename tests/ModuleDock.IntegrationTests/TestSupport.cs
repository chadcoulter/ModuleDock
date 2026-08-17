namespace ModuleDock.IntegrationTests;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "moduledock-it",
        Guid.NewGuid().ToString("N"));

    public TempDirectory()
    {
        Directory.CreateDirectory(Path);
    }

    public string AddPublishedPlugin(string publishedName, string? directoryName = null)
    {
        var destination = System.IO.Path.Combine(Path, directoryName ?? publishedName);
        CopyDirectory(PublishedPlugins.Get(publishedName), destination);
        return destination;
    }

    public void WriteManifest(string pluginDirectory, string json)
    {
        File.WriteAllText(System.IO.Path.Combine(pluginDirectory, "plugin.json"), json);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = System.IO.Path.GetRelativePath(source, file);
            var destFile = System.IO.Path.Combine(destination, relative);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite: true);
        }
    }
}

internal static class PublishedPlugins
{
    public static string Root { get; } = Path.Combine(AppContext.BaseDirectory, "testplugins");

    public static string Get(string name)
    {
        var path = Path.Combine(Root, name);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Published test plugin '{name}' was not found at '{path}'.");
        }

        return path;
    }
}
