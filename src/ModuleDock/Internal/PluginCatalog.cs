using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace ModuleDock;

internal sealed class PluginCatalog : IPluginCatalog
{
    private readonly FrozenDictionary<string, PluginDescriptor> _byId;
    private readonly FrozenDictionary<string, PluginDescriptor> _byCapability;

    public PluginCatalog(PluginLoadReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        LoadReport = report;
        Plugins = new ReadOnlyCollection<PluginDescriptor>([.. report.Loaded]);

        _byId = report.Loaded.ToFrozenDictionary(
            static plugin => plugin.Id,
            StringComparer.OrdinalIgnoreCase);

        var capabilityPairs = new List<KeyValuePair<string, PluginDescriptor>>();
        foreach (var plugin in report.Loaded)
        {
            foreach (var capability in plugin.Capabilities)
            {
                capabilityPairs.Add(new KeyValuePair<string, PluginDescriptor>(capability, plugin));
            }
        }

        _byCapability = capabilityPairs.ToFrozenDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<PluginDescriptor> Plugins { get; }

    public PluginLoadReport LoadReport { get; }

    public PluginDescriptor? FindById(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return _byId.GetValueOrDefault(pluginId);
    }

    public PluginDescriptor? FindByCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);
        return _byCapability.GetValueOrDefault(capability);
    }
}
