using System.Reflection;
using ModuleDock;

namespace ModuleDock.Internal;

internal static class SharedAssemblyNames
{
    public static HashSet<string> Create(ModuleDockOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(IPluginModule).Assembly.GetName().Name!
        };

        foreach (var assembly in options.SharedAssemblies)
        {
            var name = assembly.GetName().Name;
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        return names;
    }
}
