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
        context.IsSharedAssembly(new System.Reflection.AssemblyName("PrivateDependency")).Should().BeFalse();
    }

    [Fact]
    public void SharedAssemblyMatching_IgnoresCase()
    {
        var entry = typeof(IPluginCatalog).Assembly.Location;
        var options = new ModuleDockOptions
        {
            PluginDirectory = Path.GetTempPath(),
            ContractVersion = "1.0.0"
        };
        options.ShareAssemblyContaining<IPluginCatalog>();
        var context = new PluginLoadContext(entry, SharedAssemblyNames.Create(options));

        var name = typeof(IPluginCatalog).Assembly.GetName().Name!;
        context.IsSharedAssembly(new System.Reflection.AssemblyName(name.ToUpperInvariant()))
            .Should().BeTrue();
    }

    [Fact]
    public void FrameworkExtensions_AreRecognisedButNotTreatedAsExplicitlyShared()
    {
        var entry = typeof(IPluginCatalog).Assembly.Location;
        var options = new ModuleDockOptions
        {
            PluginDirectory = Path.GetTempPath(),
            ContractVersion = "1.0.0"
        };
        var context = new PluginLoadContext(entry, SharedAssemblyNames.Create(options));

        // Framework abstractions resolve to the host only when the host provides them,
        // so they must not short-circuit the plugin's own dependency resolution.
        var diAbstractions = typeof(IServiceCollection).Assembly.GetName();
        context.IsSharedAssembly(diAbstractions).Should().BeFalse();
        PluginLoadContext.IsFrameworkExtension(diAbstractions.Name).Should().BeTrue();
        PluginLoadContext.IsFrameworkExtension("PrivateDependency").Should().BeFalse();
    }
}
