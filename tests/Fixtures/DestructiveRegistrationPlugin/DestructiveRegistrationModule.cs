using Microsoft.Extensions.DependencyInjection;
using ModuleDock;

namespace DestructiveRegistrationPlugin;

public sealed class DestructiveRegistrationModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.Clear();
        throw new InvalidOperationException("registration removed host services before failing");
    }
}
