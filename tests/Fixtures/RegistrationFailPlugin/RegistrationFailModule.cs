using Microsoft.Extensions.DependencyInjection;
using ModuleDock;

namespace RegistrationFailPlugin;

public sealed class RegistrationFailModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        throw new InvalidOperationException("service registration failed");
    }
}
