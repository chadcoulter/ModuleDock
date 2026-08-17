using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using ModuleDock;
using ModuleDock.TestContracts;

namespace PrivateFrameworkPlugin;

public sealed class PrivateFrameworkModule : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedSingleton<ITestCalculator, PooledCalculator>(context.Capabilities.First());
    }
}

internal sealed class PooledCalculator : ITestCalculator
{
    private readonly ObjectPool<object> _pool =
        new DefaultObjectPool<object>(new DefaultPooledObjectPolicy<object>());

    public string PluginId => "private-framework-plugin";

    public string GetPrivateDependencyVersion() =>
        typeof(ObjectPool<>).Assembly.GetName().Name ?? "unknown";
}
