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
        _sharedAssemblyNames = new HashSet<string>(sharedAssemblyNames, StringComparer.Ordinal);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsSharedAssembly(assemblyName))
        {
            return null;
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
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return _sharedAssemblyNames.Contains(name)
               || name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal);
    }
}
