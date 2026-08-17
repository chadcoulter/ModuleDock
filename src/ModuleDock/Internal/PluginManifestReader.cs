using System.Text.Json;
using System.Text.Json.Serialization;
using ModuleDock;

namespace ModuleDock.Internal;

internal static class PluginManifestReader
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static bool TryRead(
        string manifestPath,
        out PluginManifestDto? dto,
        out PluginDiagnostic? diagnostic)
    {
        dto = null;
        diagnostic = null;

        string json;
        try
        {
            json = File.ReadAllText(manifestPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            diagnostic = new PluginDiagnostic(
                PluginDiagnosticCode.MalformedManifest,
                "The plugin manifest could not be read.",
                pluginPath: manifestPath);
            return false;
        }

        try
        {
            dto = JsonSerializer.Deserialize<PluginManifestDto>(json, _serializerOptions);
        }
        catch (JsonException)
        {
            diagnostic = new PluginDiagnostic(
                PluginDiagnosticCode.MalformedManifest,
                "The plugin manifest is not valid JSON.",
                pluginPath: manifestPath);
            return false;
        }

        if (dto is null)
        {
            diagnostic = new PluginDiagnostic(
                PluginDiagnosticCode.MalformedManifest,
                "The plugin manifest is empty.",
                pluginPath: manifestPath);
            return false;
        }

        return true;
    }
}

internal sealed class PluginManifestDto
{
    public string? Id { get; init; }

    public string? Version { get; init; }

    public string? EntryAssembly { get; init; }

    public string? ContractVersion { get; init; }

    public string[]? Capabilities { get; init; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; init; }

    [JsonPropertyName("checksumSha256")]
    public string? ChecksumSha256 { get; init; }

    public string? EffectiveChecksum =>
        string.IsNullOrWhiteSpace(Checksum) ? ChecksumSha256 : Checksum;
}
