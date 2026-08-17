using Microsoft.Extensions.DependencyInjection;
using ModuleDock;
using ModuleDock.TestContracts;
using PrivateDependency;

namespace VersionOnePlugin;

public sealed class VersionOnePluginModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedSingleton<ITestCalculator, VersionOneCalculator>("v1-private");
    }
}

internal sealed class VersionOneCalculator : ITestCalculator
{
    public string PluginId => "version-one-plugin";

    public string GetPrivateDependencyVersion() => VersionStamp.Version;
}
