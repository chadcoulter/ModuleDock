using Microsoft.Extensions.DependencyInjection;
using ModuleDock;

namespace MultiModulePlugin;

public sealed class FirstModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
    }
}

public sealed class SecondModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
    }
}
