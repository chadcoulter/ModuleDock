using Microsoft.Extensions.DependencyInjection;
using ModuleDock;

namespace ThrowingModulePlugin;

public sealed class ThrowingModule : IPluginModule
{
    public ThrowingModule()
    {
        throw new InvalidOperationException("module construction failed");
    }

    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
    }
}
