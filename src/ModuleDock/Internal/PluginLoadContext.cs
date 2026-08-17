using System.Reflection;
using System.Runtime.Loader;

namespace ModuleDock.Internal;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblyNames;

    public PluginLoadContext(string entryAssemblyPath, IReadOnlySet<string> sharedAssemblyNames)
        : base($"plugin:{Path.GetFileNameWithoutExtension(entryAssemblyPath)}", isCollectible: true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryAssemblyPath);
        ArgumentNullException.ThrowIfNull(sharedAssemblyNames);

        _resolver = new AssemblyDependencyResolver(entryAssemblyPath);
        _sharedAssemblyNames = new HashSet<string>(sharedAssemblyNames, StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsSharedAssembly(assemblyName))
        {
            return null;
        }

        // Framework abstractions are shared only when the host actually provides them.
        // Otherwise the plugin's private copy must win, or the load fails with nothing to resolve.
        if (IsFrameworkExtension(assemblyName.Name) && TryLoadFromHost(assemblyName, out var hostAssembly))
        {
            return hostAssembly;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }

    internal nint LoadUnmanagedDllForTests(string unmanagedDllName) => LoadUnmanagedDll(unmanagedDllName);

    internal bool IsSharedAssembly(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        return !string.IsNullOrEmpty(name) && _sharedAssemblyNames.Contains(name);
    }

    internal static bool IsFrameworkExtension(string? name) =>
        !string.IsNullOrEmpty(name)
        && name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase);

    private static bool TryLoadFromHost(AssemblyName assemblyName, out Assembly? assembly)
    {
        try
        {
            // Match on simple name so a plugin built against a different patch version
            // still binds to the host's copy and keeps type identity.
            assembly = Default.LoadFromAssemblyName(new AssemblyName(assemblyName.Name!));
            return true;
        }
        catch (Exception exception) when (exception is FileNotFoundException
                                              or FileLoadException
                                              or BadImageFormatException)
        {
            assembly = null;
            return false;
        }
    }
}
