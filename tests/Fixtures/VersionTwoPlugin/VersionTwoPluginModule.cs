using Microsoft.Extensions.DependencyInjection;
using ModuleDock;
using ModuleDock.TestContracts;
using PrivateDependency;

namespace VersionTwoPlugin;

public sealed class VersionTwoPluginModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedSingleton<ITestCalculator, VersionTwoCalculator>("v2-private");
    }
}

internal sealed class VersionTwoCalculator : ITestCalculator
{
    public string PluginId => "version-two-plugin";

    public string GetPrivateDependencyVersion() => VersionStamp.Version;
}
