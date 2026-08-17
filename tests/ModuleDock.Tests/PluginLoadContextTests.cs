using Microsoft.Extensions.DependencyInjection;
using ModuleDock.Internal;

namespace ModuleDock.Tests;

public sealed class PluginLoadContextTests
{
    [Fact]
    public void UnknownUnmanagedLibrary_ReturnsZero()
    {
        var entry = typeof(IPluginCatalog).Assembly.Location;
        File.Exists(entry).Should().BeTrue();

        var options = new ModuleDockOptions
        {
            PluginDirectory = Path.GetTempPath(),
            ContractVersion = "1.0.0"
        };
        options.ShareAssemblyContaining<IPluginCatalog>();
        var shared = SharedAssemblyNames.Create(options);
        var context = new PluginLoadContext(entry, shared);

        context.LoadUnmanagedDllForTests("moduledock-missing-native-library").Should().Be(nint.Zero);
        context.IsSharedAssembly(typeof(IPluginModule).Assembly.GetName()).Should().BeTrue();
        context.IsSharedAssembly(typeof(IPluginCatalog).Assembly.GetName()).Should().BeTrue();
        context.IsSharedAssembly(typeof(IServiceCollection).Assembly.GetName()).Should().BeTrue();
        context.IsSharedAssembly(new System.Reflection.AssemblyName("PrivateDependency")).Should().BeFalse();
    }
}
