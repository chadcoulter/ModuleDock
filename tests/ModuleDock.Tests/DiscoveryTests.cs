using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModuleDock.Internal;

namespace ModuleDock.Tests;

public sealed class DiscoveryTests
{
    [Fact]
    public void AddModuleDock_MissingDirectory_FailFastThrows()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Path, "does-not-exist");
        var services = new ServiceCollection();

        var act = () => services.AddModuleDock(options =>
        {
            options.PluginDirectory = missing;
            options.ContractVersion = "1.0.0";
        });

        var exception = act.Should().Throw<PluginValidationException>().Which;
        exception.Diagnostics.Should().ContainSingle(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.MissingPluginDirectory);
        exception.Message.Should().Contain(missing);
        exception.Diagnostics[0].PluginPath.Should().Be(Path.GetFullPath(missing));
    }

    [Fact]
    public void AddModuleDock_MissingDirectory_SkipInvalidRegistersEmptyCatalogue()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Path, "does-not-exist");
        var services = new ServiceCollection();

        services.AddModuleDock(options =>
        {
            options.PluginDirectory = missing;
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IPluginCatalog>();
        catalog.Plugins.Should().BeEmpty();
        catalog.LoadReport.Diagnostics.Should().ContainSingle(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.MissingPluginDirectory);
    }

    [Fact]
    public void AddModuleDock_MissingManifest_IsReported()
    {
        using var temp = new TempDirectory();
        temp.CreatePluginDirectory("empty-plugin");

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.MissingManifest);
    }

    [Fact]
    public void AddModuleDock_MalformedJson_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("broken", "{ not json");

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.MalformedManifest);
    }

    [Fact]
    public void AddModuleDock_MissingRequiredFields_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("incomplete", """{ "id": "incomplete" }""");

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.MissingRequiredField);
    }

    [Fact]
    public void AddModuleDock_InvalidPluginId_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("bad-id", TestJson.Manifest(id: "../evil"));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.InvalidPluginId);
    }

    [Fact]
    public void AddModuleDock_InvalidVersion_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("bad-version", TestJson.Manifest(version: "not-a-version"));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.InvalidVersion);
    }

    [Fact]
    public void AddModuleDock_ContractMismatch_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("future", TestJson.Manifest(id: "future-plugin", contractVersion: "9.0.0"));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.ContractMismatch, hostContract: "1.0.0");
    }

    [Fact]
    public void AddModuleDock_RootedEntryPath_IsReported()
    {
        using var temp = new TempDirectory();
        var rooted = Path.Combine(Path.GetTempPath(), "outside.dll");
        temp.WriteManifest("rooted", TestJson.Manifest(id: "rooted-plugin", entryAssembly: rooted));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.RootedEntryPath);
    }

    [Fact]
    public void AddModuleDock_PathTraversal_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("traverse", TestJson.Manifest(id: "traverse-plugin", entryAssembly: "../outside.dll"));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.PathTraversal);
    }

    [Fact]
    public void AddModuleDock_MissingEntryAssembly_IsReported()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("missing-entry", TestJson.Manifest(id: "missing-entry"));

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.MissingEntryAssembly);
    }

    [Fact]
    public void AddModuleDock_MissingDependencyMetadata_IsReported()
    {
        using var temp = new TempDirectory();
        var directory = temp.WriteManifest("no-deps", TestJson.Manifest(id: "no-deps", entryAssembly: "NoDeps.dll"));
        File.WriteAllBytes(Path.Combine(directory, "NoDeps.dll"), [0]);

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.MissingDependencyMetadata);
    }

    [Fact]
    public void AddModuleDock_ChecksumMismatch_IsReported()
    {
        using var temp = new TempDirectory();
        var directory = temp.WriteManifest(
            "checksum",
            TestJson.Manifest(
                id: "checksum-plugin",
                entryAssembly: "Checksum.dll",
                checksum: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        File.WriteAllBytes(Path.Combine(directory, "Checksum.dll"), [1, 2, 3]);
        File.WriteAllText(Path.Combine(directory, "Checksum.deps.json"), "{}");

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.ChecksumMismatch);
    }

    [Fact]
    public void AddModuleDock_InvalidChecksum_IsReported()
    {
        using var temp = new TempDirectory();
        var directory = temp.WriteManifest(
            "checksum",
            TestJson.Manifest(
                id: "checksum-plugin",
                entryAssembly: "Checksum.dll",
                checksum: "not-hex"));
        File.WriteAllBytes(Path.Combine(directory, "Checksum.dll"), [1, 2, 3]);
        File.WriteAllText(Path.Combine(directory, "Checksum.deps.json"), "{}");

        AssertDiagnostic(temp.Path, PluginDiagnosticCode.InvalidChecksum);
    }

    [Fact]
    public void AddModuleDock_SkipInvalid_LeavesValidEmptyCatalogueAndKeepsDiagnostics()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("broken", "{ not json");
        var services = new ServiceCollection();

        services.AddModuleDock(options =>
        {
            options.PluginDirectory = temp.Path;
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IPluginCatalog>();
        catalog.Plugins.Should().BeEmpty();
        catalog.LoadReport.Diagnostics.Should().NotBeEmpty();
        catalog.LoadReport.FailurePolicy.Should().Be(PluginFailurePolicy.SkipInvalid);
    }

    [Fact]
    public void AddModuleDock_CalledTwice_Throws()
    {
        using var temp = new TempDirectory();
        var services = new ServiceCollection();
        services.AddModuleDock(options =>
        {
            options.PluginDirectory = temp.Path;
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
        });

        var act = () => services.AddModuleDock(options =>
        {
            options.PluginDirectory = temp.Path;
            options.ContractVersion = "1.0.0";
        });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddModuleDock_RequiresPluginDirectoryAndContractVersion()
    {
        var services = new ServiceCollection();

        var missingDirectory = () => services.AddModuleDock(static options => options.ContractVersion = "1.0.0");
        var missingContract = () => services.AddModuleDock(static options => options.PluginDirectory = "plugins");

        missingDirectory.Should().Throw<ArgumentException>();
        missingContract.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HostBuilder_ResolvesRelativePluginDirectoryAgainstContentRoot()
    {
        using var temp = new TempDirectory();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = temp.Path
        });

        builder.AddModuleDock(options =>
        {
            options.PluginDirectory = "plugins";
            options.ContractVersion = "1.0.0";
            options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
        });

        using var host = builder.Build();
        var catalog = host.Services.GetRequiredService<IPluginCatalog>();
        catalog.LoadReport.PluginDirectory.Should().Be(Path.GetFullPath(Path.Combine(temp.Path, "plugins")));
        catalog.LoadReport.Diagnostics.Should().ContainSingle(static diagnostic =>
            diagnostic.Code == PluginDiagnosticCode.MissingPluginDirectory);
    }

    [Fact]
    public void Diagnostics_DoNotContainRawFileContents()
    {
        using var temp = new TempDirectory();
        temp.WriteManifest("secret", """{ "token": "super-secret-value", """);

        try
        {
            new ServiceCollection().AddModuleDock(options =>
            {
                options.PluginDirectory = temp.Path;
                options.ContractVersion = "1.0.0";
            });
        }
        catch (PluginValidationException exception)
        {
            exception.Message.Should().NotContain("super-secret-value");
            exception.Diagnostics.Should().OnlyContain(static diagnostic =>
                !diagnostic.Message.Contains("super-secret-value", StringComparison.Ordinal));
            return;
        }

        Assert.Fail("Expected PluginValidationException.");
    }

    private static void AssertDiagnostic(
        string pluginRoot,
        PluginDiagnosticCode code,
        string hostContract = "1.0.0")
    {
        var services = new ServiceCollection();
        var act = () => services.AddModuleDock(options =>
        {
            options.PluginDirectory = pluginRoot;
            options.ContractVersion = hostContract;
        });

        var exception = act.Should().Throw<PluginValidationException>().Which;
        exception.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == code);
        exception.Diagnostics.Should().OnlyContain(static diagnostic =>
            !string.IsNullOrWhiteSpace(diagnostic.Message));
    }
}

public sealed class CatalogueTests
{
    [Fact]
    public void PluginCatalog_IsImmutableAfterConstruction()
    {
        var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "marine" };
        var descriptor = new PluginDescriptor(
            "marine-rating",
            "1.0.0",
            "1.0.0",
            capabilities,
            "/plugins/marine-rating",
            "MarineRating.Plugin.dll");
        var report = new PluginLoadReport(
            "/plugins",
            PluginFailurePolicy.FailFast,
            "1.0.0",
            [descriptor],
            []);
        var catalog = new PluginCatalog(report);

        catalog.Plugins.Should().HaveCount(1);
        catalog.FindById("MARINE-RATING").Should().BeSameAs(descriptor);
        catalog.FindByCapability("Marine").Should().BeSameAs(descriptor);
        ((System.Collections.IList)catalog.Plugins).IsReadOnly.Should().BeTrue();
        ((System.Collections.IList)catalog.LoadReport.Loaded).IsReadOnly.Should().BeTrue();
        ((System.Collections.IList)catalog.LoadReport.Diagnostics).IsReadOnly.Should().BeTrue();
    }
}
