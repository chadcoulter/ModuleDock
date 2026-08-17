using Microsoft.Extensions.DependencyInjection;
using ModuleDock.Internal;

namespace ModuleDock.Tests;

public sealed class PluginVersionTests
{
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0")]
    [InlineData("2.3.4.5")]
    public void TryParse_AcceptsVersionValues(string value)
    {
        PluginVersion.TryParse(value, out var version).Should().BeTrue();
        version.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("latest")]
    [InlineData("1")]
    [InlineData("1.0.0-beta")]
    public void TryParse_RejectsInvalidValues(string? value)
    {
        PluginVersion.TryParse(value, out _).Should().BeFalse();
    }

    [Fact]
    public void Compare_NormalizesMissingBuildNumbers()
    {
        PluginVersion.Compare("1.2", "1.2.0").Should().Be(0);
    }
}

public sealed class PluginIdValidatorTests
{
    [Theory]
    [InlineData("marine-rating")]
    [InlineData("A")]
    [InlineData("plugin.v2")]
    [InlineData("plugin_1")]
    public void IsValid_AcceptsSafeIdentifiers(string id)
    {
        PluginIdValidator.IsValid(id).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../evil")]
    [InlineData("plugin/id")]
    [InlineData("plugin\\id")]
    [InlineData("-leading")]
    public void IsValid_RejectsUnsafeIdentifiers(string id)
    {
        PluginIdValidator.IsValid(id).Should().BeFalse();
    }
}

public sealed class PluginPathValidatorTests
{
    [Fact]
    public void IsInsideDirectory_AcceptsFilesInThePluginFolder()
    {
        using var temp = new TempDirectory();
        var pluginDir = temp.CreatePluginDirectory("marine-rating");
        var entry = Path.Combine(pluginDir, "MarineRating.Plugin.dll");
        File.WriteAllText(entry, "dll");

        PluginPathValidator.IsInsideDirectory(entry, pluginDir).Should().BeTrue();
    }

    [Fact]
    public void ContainsTraversal_DetectsParentSegments()
    {
        PluginPathValidator.ContainsTraversal("../evil.dll").Should().BeTrue();
        PluginPathValidator.ContainsTraversal("nested/../evil.dll").Should().BeTrue();
        PluginPathValidator.ContainsTraversal("MarineRating.Plugin.dll").Should().BeFalse();
    }
}

public sealed class PluginChecksumTests
{
    [Fact]
    public void Matches_AcceptsHexAndSha256Prefix()
    {
        using var temp = new TempDirectory();
        var file = Path.Combine(temp.Path, "entry.dll");
        File.WriteAllBytes(file, [1, 2, 3, 4]);
        var hex = PluginChecksum.ComputeSha256Hex(file);

        PluginChecksum.Matches(file, hex).Should().BeTrue();
        PluginChecksum.Matches(file, "sha256:" + hex.ToLowerInvariant()).Should().BeTrue();
        PluginChecksum.IsWellFormed(hex).Should().BeTrue();
        PluginChecksum.IsWellFormed("not-a-hash").Should().BeFalse();
    }
}

public sealed class PluginManifestReaderTests
{
    [Fact]
    public void TryRead_ParsesCamelCaseManifest()
    {
        using var temp = new TempDirectory();
        var directory = temp.WriteManifest("marine-rating", TestJson.Manifest("marine-rating"));
        var path = Path.Combine(directory, "plugin.json");

        PluginManifestReader.TryRead(path, out var dto, out var diagnostic).Should().BeTrue();
        diagnostic.Should().BeNull();
        dto!.Id.Should().Be("marine-rating");
        dto.EntryAssembly.Should().Be("Sample.Plugin.dll");
        dto.Capabilities.Should().Equal("sample");
    }

    [Fact]
    public void TryRead_ReturnsMalformedManifestForInvalidJson()
    {
        using var temp = new TempDirectory();
        var directory = temp.WriteManifest("broken", "{ not json");
        var path = Path.Combine(directory, "plugin.json");

        PluginManifestReader.TryRead(path, out var dto, out var diagnostic).Should().BeFalse();
        dto.Should().BeNull();
        diagnostic!.Code.Should().Be(PluginDiagnosticCode.MalformedManifest);
        diagnostic.PluginPath.Should().Be(path);
    }
}
