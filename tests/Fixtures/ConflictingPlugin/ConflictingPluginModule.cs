using Microsoft.Extensions.DependencyInjection;
using ModuleDock;
using ModuleDock.TestContracts;

namespace ConflictingPlugin;

public sealed class ConflictingPluginModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedSingleton<ITestCalculator, ConflictingCalculator>("conflict");
    }
}

internal sealed class ConflictingCalculator : ITestCalculator
{
    public string PluginId => "conflicting-plugin";

    public string GetPrivateDependencyVersion() => "none";
}
