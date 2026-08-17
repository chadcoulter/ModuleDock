namespace ModuleDock.Tests;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "moduledock-tests",
        Guid.NewGuid().ToString("N"));

    public TempDirectory()
    {
        Directory.CreateDirectory(Path);
    }

    public string CreatePluginDirectory(string name)
    {
        var directory = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public string WriteManifest(string pluginFolder, string json)
    {
        var directory = CreatePluginDirectory(pluginFolder);
        var manifestPath = System.IO.Path.Combine(directory, "plugin.json");
        File.WriteAllText(manifestPath, json);
        return directory;
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
}

internal static class TestJson
{
    public static string Manifest(
        string id = "sample-plugin",
        string version = "1.0.0",
        string entryAssembly = "Sample.Plugin.dll",
        string contractVersion = "1.0.0",
        string? capabilities = """["sample"]""",
        string? checksum = null)
    {
        var checksumJson = checksum is null ? string.Empty : $""",{Environment.NewLine}  "checksum": "{checksum}" """;
        return $$"""
            {
              "id": "{{id}}",
              "version": "{{version}}",
              "entryAssembly": "{{entryAssembly}}",
              "contractVersion": "{{contractVersion}}",
              "capabilities": {{capabilities ?? "[]"}}{{checksumJson}}
            }
            """;
    }
}
