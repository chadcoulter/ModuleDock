using Microsoft.Extensions.DependencyInjection;
using ModuleDock;
using ModuleDock.TestContracts;

namespace ValidPlugin;

public sealed class ValidPluginModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedSingleton<ITestCalculator, ValidCalculator>(context.Capabilities.First());
    }
}

internal sealed class ValidCalculator : ITestCalculator
{
    public string PluginId => "valid-plugin";

    public string GetPrivateDependencyVersion() => "none";
}
