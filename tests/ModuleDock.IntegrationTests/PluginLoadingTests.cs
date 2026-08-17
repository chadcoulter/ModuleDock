using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using ModuleDock.Internal;
using ModuleDock.TestContracts;

namespace ModuleDock.IntegrationTests;

public sealed class PluginLoadingTests
{
    [Fact]
    public void ValidPlugin_RegistersKeyedServiceAndCatalogue()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var services = CreateServices(temp.Path);

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IPluginCatalog>();
        catalog.Plugins.Should().ContainSingle(static plugin => plugin.Id == "valid-plugin");
        catalog.FindByCapability("VALID").Should().NotBeNull();

        var calculator = provider.GetRequiredKeyedService<ITestCalculator>("valid");
        calculator.PluginId.Should().Be("valid-plugin");
        typeof(ITestCalculator).IsAssignableFrom(calculator.GetType()).Should().BeTrue();
        calculator.GetType().GetInterfaces().Should().Contain(typeof(ITestCalculator));
    }

    [Fact]
    public void SharedContract_UsesHostTypeIdentity()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var services = CreateServices(temp.Path);

        using var provider = services.BuildServiceProvider();
        var calculator = provider.GetRequiredKeyedService<ITestCalculator>("valid");
        calculator.Should().BeAssignableTo<ITestCalculator>();
        calculator.GetType().Assembly.GetName().Name.Should().Be("ValidPlugin");
        typeof(ITestCalculator).Assembly.GetName().Name.Should().Be("ModuleDock.TestContracts");
    }

    [Fact]
    public void TwoPlugins_CanLoadDifferentPrivateDependencyVersions()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("VersionOnePlugin", "version-one");
        temp.AddPublishedPlugin("VersionTwoPlugin", "version-two");
        var services = CreateServices(temp.Path);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredKeyedService<ITestCalculator>("v1-private");
        var second = provider.GetRequiredKeyedService<ITestCalculator>("v2-private");

        first.GetPrivateDependencyVersion().Should().Be("1.0.0");
        second.GetPrivateDependencyVersion().Should().Be("2.0.0");
        first.GetType().Assembly.Should().NotBeSameAs(second.GetType().Assembly);
    }

    [Fact]
    public void DuplicateIds_FailFast()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "first");
        var second = temp.AddPublishedPlugin("ValidPlugin", "second");
        File.WriteAllText(
            Path.Combine(second, "plugin.json"),
            """
            {
              "id": "valid-plugin",
              "version": "1.0.1",
              "entryAssembly": "ValidPlugin.dll",
              "contractVersion": "1.0.0",
              "capabilities": ["other"]
            }
            """);

        var act = () => CreateServices(temp.Path);
        var exception = act.Should().Throw<PluginValidationException>().Which;
        exception.Diagnostics.Should().Contain(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.DuplicatePluginId);
    }

    [Fact]
    public void DuplicateCapabilities_FailFast()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        temp.AddPublishedPlugin("ConflictingPlugin", "conflicting-plugin");

        var act = () => CreateServices(temp.Path);
        act.Should().Throw<PluginValidationException>()
            .Which.Diagnostics.Should().Contain(static diagnostic =>
                diagnostic.Code == PluginDiagnosticCode.DuplicateCapability
                && string.Equals(diagnostic.Capability, "valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DuplicateCapabilities_SkipInvalid_KeepsFirstPluginInDirectoryOrder()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        temp.AddPublishedPlugin("ConflictingPlugin", "conflicting-plugin");
        var services = CreateServices(temp.Path, PluginFailurePolicy.SkipInvalid);

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IPluginCatalog>();
        catalog.Plugins.Should().ContainSingle();
        catalog.FindById("conflicting-plugin").Should().NotBeNull();
        catalog.FindById("valid-plugin").Should().BeNull();
        catalog.LoadReport.Diagnostics.Should().Contain(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.DuplicateCapability);
    }

    [Fact]
    public void ExactlyOnePluginModule_IsRequired()
    {
        using var none = new TempDirectory();
        none.AddPublishedPlugin("NoModulePlugin", "no-module");
        var noneAct = () => CreateServices(none.Path);
        noneAct.Should().Throw<PluginValidationException>()
            .Which.Diagnostics.Should().Contain(static diagnostic =>
                diagnostic.Code == PluginDiagnosticCode.NoPluginModule);

        using var multi = new TempDirectory();
        multi.AddPublishedPlugin("MultiModulePlugin", "multi-module");
        var multiAct = () => CreateServices(multi.Path);
        multiAct.Should().Throw<PluginValidationException>()
            .Which.Diagnostics.Should().Contain(static diagnostic =>
                diagnostic.Code == PluginDiagnosticCode.MultiplePluginModules);
    }

    [Fact]
    public void ModuleConstructionFailure_PreservesInnerException()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ThrowingModulePlugin", "throwing");

        var act = () => CreateServices(temp.Path);
        var exception = act.Should().Throw<PluginValidationException>().Which;
        exception.Diagnostics.Should().Contain(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.ModuleConstructionFailed);
        exception.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("module construction failed");
    }

    [Fact]
    public void ServiceRegistrationFailure_PreservesInnerExceptionAndRollsBack()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        temp.AddPublishedPlugin("RegistrationFailPlugin", "registration-fail");

        var failFast = () => CreateServices(temp.Path);
        var exception = failFast.Should().Throw<PluginValidationException>().Which;
        exception.InnerException.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("service registration failed");

        var services = CreateServices(temp.Path, PluginFailurePolicy.SkipInvalid);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredKeyedService<ITestCalculator>("valid").Should().NotBeNull();
        provider.GetRequiredService<IPluginCatalog>().FindById("registration-fail-plugin").Should().BeNull();
    }

    [Fact]
    public void MatchingChecksum_AllowsThePluginToLoad()
    {
        using var temp = new TempDirectory();
        var directory = temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var entry = Path.Combine(directory, "ValidPlugin.dll");
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(entry)));
        File.WriteAllText(
            Path.Combine(directory, "plugin.json"),
            $$"""
            {
              "id": "valid-plugin",
              "version": "1.0.0",
              "entryAssembly": "ValidPlugin.dll",
              "contractVersion": "1.0.0",
              "capabilities": ["valid"],
              "checksum": "{{hash}}"
            }
            """);

        var services = CreateServices(temp.Path);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IPluginCatalog>().FindById("valid-plugin").Should().NotBeNull();
    }

    [Fact]
    public void EntryAssemblyOutsidePluginDirectory_IsRejected()
    {
        using var temp = new TempDirectory();
        var plugin = temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var other = temp.AddPublishedPlugin("ValidPlugin", "other-plugin");
        var relative = Path.GetRelativePath(plugin, Path.Combine(other, "ValidPlugin.dll"));
        File.WriteAllText(
            Path.Combine(plugin, "plugin.json"),
            $$"""
            {
              "id": "valid-plugin",
              "version": "1.0.0",
              "entryAssembly": "{{relative.Replace("\\", "\\\\")}}",
              "contractVersion": "1.0.0",
              "capabilities": ["valid"]
            }
            """);

        var act = () => CreateServices(temp.Path);
        var exception = act.Should().Throw<PluginValidationException>().Which;
        exception.Diagnostics.Should().Contain(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.PathTraversal
            || diagnostic.Code == PluginDiagnosticCode.EntryAssemblyOutsidePluginDirectory);
    }

    [Fact]
    public void IdsAndCapabilities_AreComparedCaseInsensitively()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var services = CreateServices(temp.Path);
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IPluginCatalog>();
        catalog.FindById("Valid-Plugin").Should().NotBeNull();
        catalog.FindByCapability("Valid").Should().NotBeNull();
    }

    [Fact]
    public void PluginLoadContext_ResolvesUnmanagedLibrariesWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var directory = temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var entry = Path.Combine(directory, "ValidPlugin.dll");
        var options = new ModuleDockOptions
        {
            PluginDirectory = temp.Path,
            ContractVersion = "1.0.0"
        };
        options.ShareAssemblyContaining<ITestCalculator>();
        var context = new PluginLoadContext(entry, SharedAssemblyNames.Create(options));
        context.LoadUnmanagedDllForTests("moduledock-missing-native-library").Should().Be(nint.Zero);
    }

    [Fact]
    public void PrivateFrameworkDependency_LoadsFromThePluginWhenTheHostLacksIt()
    {
        // The assertion below is only meaningful while the host itself cannot supply
        // this assembly, so the precondition is checked explicitly.
        var hostHasObjectPool = AppDomain.CurrentDomain.GetAssemblies()
            .Any(static assembly => assembly.GetName().Name == "Microsoft.Extensions.ObjectPool");
        hostHasObjectPool.Should().BeFalse("the fixture relies on the host not referencing ObjectPool");

        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("PrivateFrameworkPlugin", "private-framework");
        var services = CreateServices(temp.Path);

        using var provider = services.BuildServiceProvider();
        var calculator = provider.GetRequiredKeyedService<ITestCalculator>("private-framework");
        calculator.GetPrivateDependencyVersion().Should().Be("Microsoft.Extensions.ObjectPool");
    }

    [Fact]
    public void DestructiveRegistration_RestoresHostServices()
    {
        using var temp = new TempDirectory();
        temp.AddPublishedPlugin("DestructiveRegistrationPlugin", "destructive");

        var services = new ServiceCollection();
        services.AddSingleton(new HostMarker());
        var before = services.Count;

        services.AddModuleDock(options =>
        {
            options.PluginDirectory = temp.Path;
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
            options.ShareAssemblyContaining<ITestCalculator>();
        });

        // The plugin cleared the collection before throwing, so anything registered by
        // the host must have been put back.
        services.Count.Should().BeGreaterThanOrEqualTo(before);
        using var provider = services.BuildServiceProvider();
        provider.GetService<HostMarker>().Should().NotBeNull();
        provider.GetRequiredService<IPluginCatalog>().Plugins.Should().BeEmpty();
    }

    [Fact]
    public void NestedEntryAssembly_IsRecordedRelativeToThePluginDirectory()
    {
        using var temp = new TempDirectory();
        var directory = temp.AddPublishedPlugin("ValidPlugin", "valid-plugin");
        var nested = Path.Combine(directory, "bin");
        Directory.CreateDirectory(nested);
        foreach (var file in Directory.GetFiles(directory, "ValidPlugin.*"))
        {
            File.Move(file, Path.Combine(nested, Path.GetFileName(file)));
        }

        File.WriteAllText(
            Path.Combine(directory, "plugin.json"),
            """
            {
              "id": "valid-plugin",
              "version": "1.0.0",
              "entryAssembly": "bin/ValidPlugin.dll",
              "contractVersion": "1.0.0",
              "capabilities": ["valid"]
            }
            """);

        var services = CreateServices(temp.Path);
        using var provider = services.BuildServiceProvider();
        var descriptor = provider.GetRequiredService<IPluginCatalog>().FindById("valid-plugin");

        descriptor.Should().NotBeNull();
        File.Exists(Path.Combine(descriptor!.Directory, descriptor.EntryAssembly)).Should().BeTrue();
    }

    private sealed class HostMarker;

    private static ServiceCollection CreateServices(
        string pluginDirectory,
        PluginFailurePolicy failurePolicy = PluginFailurePolicy.FailFast)
    {
        var services = new ServiceCollection();
        services.AddModuleDock(options =>
        {
            options.PluginDirectory = pluginDirectory;
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = failurePolicy;
            options.ShareAssemblyContaining<ITestCalculator>();
        });
        return services;
    }
}
