using System.Text.Json;

namespace ModuleDock.Internal;

internal static class PluginDependencyMetadata
{
    /// <summary>
    /// Checks that a <c>.deps.json</c> file has the shape the runtime expects.
    /// </summary>
    /// <remarks>
    /// hostpolicy terminates the process when a dependency file parses as JSON but omits
    /// <c>runtimeTarget</c>, and that failure cannot be caught by
    /// <see cref="System.Runtime.Loader.AssemblyDependencyResolver"/> callers. The shape is
    /// therefore checked here, before the resolver is constructed, so a corrupt plugin
    /// produces a diagnostic instead of taking the host down.
    /// </remarks>
    public static bool IsUsable(string depsPath, out string reason)
    {
        try
        {
            using var stream = File.OpenRead(depsPath);
            using var document = JsonDocument.Parse(
                stream,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                reason = "is not a JSON object";
                return false;
            }

            if (!root.TryGetProperty("runtimeTarget", out var runtimeTarget)
                || runtimeTarget.ValueKind != JsonValueKind.Object
                || !runtimeTarget.TryGetProperty("name", out var targetName)
                || targetName.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(targetName.GetString()))
            {
                reason = "does not declare a runtimeTarget name";
                return false;
            }

            if (!HasObject(root, "targets") || !HasObject(root, "libraries"))
            {
                reason = "does not declare targets and libraries";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            reason = "is not valid JSON";
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            reason = "could not be read";
            return false;
        }
    }

    private static bool HasObject(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object;
}
