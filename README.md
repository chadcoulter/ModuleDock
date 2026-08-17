![ModuleDock](https://raw.githubusercontent.com/kearns2000/ModuleDock/main/icon.png)

# ModuleDock

[![CI](https://github.com/kearns2000/ModuleDock/actions/workflows/ci.yml/badge.svg)](https://github.com/kearns2000/ModuleDock/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ModuleDock.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ModuleDock)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ModuleDock.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/ModuleDock)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/github/license/kearns2000/ModuleDock)](LICENSE)

**Target framework:** 'net10.0' | **Language:** C# 14 | **Test runner:** xUnit

ModuleDock is an opinionated, manifest-driven startup plugin host for .NET 10 applications that use the Generic Host and dependency injection.

It is not merely a wrapper around 'AssemblyLoadContext'. The .NET runtime already provides the loading primitives. ModuleDock adds the conventions, validation, Generic Host integration and deployment model needed to use those primitives consistently.

## The problem ModuleDock solves

A production plugin host has to do more than load a DLL and call an interface. The host and plugin need a stable contract. Dependencies must resolve from the correct location. Two plugins may require incompatible versions of the same library. Duplicate capabilities must be detected before the first request. Invalid plugins need a deliberate failure policy. None of that falls out of 'AssemblyLoadContext' on its own.

ModuleDock is for trusted, independently packaged capabilities that are selected when the process starts: product-specific algorithms, import adapters, report renderers, command sets, and similar modules. If every plugin is built and deployed with the host, ordinary projects may be enough. If plugins are untrusted, use process or container isolation instead.

## Why it exists

The design follows [Designing a Plugin Architecture in .NET 10](https://dotnetdigest.com/designing-a-plugin-architecture-in-net-10). That article shows a rating API that loads marine (and later aviation or property) plugins without taking a project reference to those plugins. ModuleDock extracts the reusable host: per-plugin directories, a validated 'plugin.json' manifest, contract compatibility, duplicate detection, one load context per plugin, private dependency isolation, and registration into 'IServiceCollection' before 'builder.Build()'.

Version 1 is intentionally a startup-and-restart system. Installing or upgrading a plugin means replacing its directory and restarting the host.

## Installation

```bash
dotnet add package ModuleDock
dotnet add package ModuleDock.Abstractions
```

Host applications install 'ModuleDock'. Plugin authors install 'ModuleDock.Abstractions' plus the application's own contract assembly. They should not reference 'ModuleDock' itself.

## Five-minute quick start

1. Put the application contract (interfaces and DTOs only) in its own assembly.
2. Call 'AddModuleDock' on the Generic Host **before** 'builder.Build()'.
3. Ship each plugin as a directory containing 'plugin.json', the entry assembly, '.deps.json' and private dependencies.
4. Restart the host after replacing a plugin directory.

```csharp
builder.AddModuleDock(options =>
{
    options.PluginDirectory = Path.Combine(
        builder.Environment.ContentRootPath,
        "plugins");

    options.ContractVersion = "1.0.0";
    options.FailurePolicy = PluginFailurePolicy.FailFast;
    options.ShareAssemblyContaining<IRiskCalculator>();
});
```

## Host configuration

'AddModuleDock' works on 'IHostApplicationBuilder', so it supports worker services and ASP.NET Core alike. Relative plugin directories are resolved against the content root. You can also call 'IServiceCollection.AddModuleDock' directly.

Always share:

- 'ModuleDock.Abstractions' (automatic)
- the application contract assemblies you pass to 'ShareAssemblyContaining<T>()'
- 'Microsoft.Extensions.*' abstractions that cross the host/plugin boundary (automatic)

Do not share private plugin dependencies. Those should resolve inside each plugin's load context.

## Plugin implementation

A plugin module has a public parameterless constructor and registers services before the root provider exists:

```csharp
public sealed class MarineRatingPlugin : IPluginModule
{
    public void ConfigureServices(
        IServiceCollection services,
        PluginContext context)
    {
        services.AddKeyedScoped<IRiskCalculator, MarineRiskCalculator>(
            "marine");
    }
}
```

Keep the plugin project targeted at 'net10.0', set '<EnableDynamicLoading>true</EnableDynamicLoading>', and exclude shared contracts from the plugin output:

```xml
<ProjectReference Include="..\QuoteHost.Contracts\QuoteHost.Contracts.csproj">
  <Private>false</Private>
  <ExcludeAssets>runtime</ExcludeAssets>
</ProjectReference>
<ProjectReference Include="..\..\src\ModuleDock.Abstractions\ModuleDock.Abstractions.csproj">
  <Private>false</Private>
  <ExcludeAssets>runtime</ExcludeAssets>
</ProjectReference>
```

The host must not take a project or assembly reference to the plugin. If it did, the plugin would become an ordinary host dependency.

## plugin.json

```json
{
  "id": "marine-rating",
  "version": "1.0.0",
  "entryAssembly": "MarineRating.Plugin.dll",
  "contractVersion": "1.0.0",
  "capabilities": [
    "marine"
  ]
}
```

An optional 'checksum' field may contain the SHA-256 of the entry assembly as 64 hex characters, optionally prefixed with 'sha256:'. Version 1 does not implement code signing.

IDs and capabilities are compared case-insensitively. Plugin identifiers must start with a letter or digit and may contain letters, digits, '.', '_' or '-'.

## Deployment directory layout

```text
plugins/
  marine-rating/
    plugin.json
    MarineRating.Plugin.dll
    MarineRating.Plugin.deps.json
    MarineRiskModel.dll
  aviation-rating/
    plugin.json
    AviationRating.Plugin.dll
    AviationRating.Plugin.deps.json
```

Each plugin lives in its own directory. ModuleDock does not treat every DLL under the plugin root as executable plugin code.

Publish a plugin with:

```bash
dotnet publish samples/Plugins/MarineRating.Plugin -c Release -o samples/QuoteHost/plugins/marine-rating
```

Building 'QuoteHost' also deploys the marine plugin through the 'DeployMarinePlugin' target.

## Plugin loading sequence

![Load sequence](https://raw.githubusercontent.com/kearns2000/ModuleDock/main/docs/load-sequence.svg)

1. Discover one subdirectory per plugin and read 'plugin.json'.
2. Validate required fields, versions, contract compatibility, entry paths, '.deps.json' and optional checksums.
3. Create one collectible 'AssemblyLoadContext' per plugin.
4. Share host contracts; resolve private assemblies through 'AssemblyDependencyResolver'.
5. Require exactly one concrete 'IPluginModule', construct it, and call 'ConfigureServices'.
6. Detect duplicate plugin IDs and capabilities.
7. Register an immutable 'IPluginCatalog' and 'PluginLoadReport'.
8. Call 'builder.Build()'.

## Architecture

![Architecture](https://raw.githubusercontent.com/kearns2000/ModuleDock/main/docs/architecture.svg)

Compile-time dependencies flow in one direction: plugins reference the contract and 'ModuleDock.Abstractions'; the host references the contract and 'ModuleDock'. The host never references a plugin project.

## Dependency isolation

Each plugin gets its own 'AssemblyLoadContext' and an 'AssemblyDependencyResolver' bound to its entry assembly and '.deps.json'. Managed and unmanaged dependencies resolve from the plugin directory when they are private to that plugin.

Returning 'null' from 'Load' for shared assemblies lets the default context supply the host's copy. That is how 'IPluginModule', 'IServiceCollection' and the application contract keep a single type identity.

ModuleDock does not place every plugin dependency into the default context, and it does not blindly share arbitrary third-party assemblies.

## Shared contract behaviour

If the plugin copied 'QuoteHost.Contracts.dll' into its folder, the plugin and host would load two assemblies with the same types. 'GetKeyedService<IRiskCalculator>("marine")' would then fail even though the plugin compiled cleanly. 'Private=false' / 'ExcludeAssets=runtime' plus 'ShareAssemblyContaining<IRiskCalculator>()' keep one type identity.

Integration tests load two plugins that depend on different versions of the same private assembly and assert that both run in one host.

## Validation and diagnostics

Each diagnostic identifies the plugin, path, a stable 'PluginDiagnosticCode' and a readable explanation. Diagnostics do not include file contents or configuration secrets.

Validated conditions include:

- missing or malformed manifests
- required values, plugin IDs and version values
- contract compatibility
- entry assembly existence and '.deps.json' availability
- rooted entry paths, '..' traversal, and entry assemblies outside the plugin directory
- duplicate plugin IDs and capabilities (case-insensitive)
- zero or multiple concrete 'IPluginModule' implementations
- module construction and service-registration failures
- checksum mismatch when a checksum is supplied

Exceptions thrown by a plugin are wrapped with plugin ID and path information. The original exception is preserved as 'InnerException'.

## Failure-policy examples

'PluginFailurePolicy.FailFast' (default) throws 'PluginValidationException' before the host is built. Use this when every plugin is required.

```csharp
options.FailurePolicy = PluginFailurePolicy.FailFast;
```

'PluginFailurePolicy.SkipInvalid' excludes invalid plugins and keeps diagnostics on 'PluginLoadReport' / 'IPluginCatalog.LoadReport'. Successfully registered plugins remain available.

```csharp
options.FailurePolicy = PluginFailurePolicy.SkipInvalid;
```

## Configuration reference

| Option | Meaning |
| --- | --- |
| 'PluginDirectory' | Directory that contains one subdirectory per plugin. Relative paths on 'IHostApplicationBuilder' resolve against the content root. |
| 'ContractVersion' | Contract version provided by the host. A plugin loads when its 'contractVersion' is less than or equal to this value. |
| 'FailurePolicy' | 'FailFast' or 'SkipInvalid'. |
| 'RequireDependencyMetadata' | Require a '.deps.json' file next to the entry assembly. Default is 'true'. |
| 'ShareAssemblyContaining<T>()' | Share the assembly that contains 'T' with every plugin load context. |

The catalogue is fixed after startup. There is no 'UnloadAsync' API.

## Package responsibilities

| Package | Responsibility |
| --- | --- |
| 'ModuleDock.Abstractions' | 'IPluginModule' and 'PluginContext'. Plugin authors reference this. |
| 'ModuleDock' | Discovery, validation, load contexts, Generic Host integration, catalogue and diagnostics. |

Application-specific types such as 'IRiskCalculator' belong in the host contract assembly ('QuoteHost.Contracts' in the sample), never in ModuleDock.

## Sample application

'samples/QuoteHost' is an ASP.NET Core 10 minimal API. It has no project reference to 'MarineRating.Plugin'. Keyed DI selects the marine calculator:

```bash
dotnet run --project samples/QuoteHost --urls http://127.0.0.1:5088
```

```bash
curl http://127.0.0.1:5088/plugins
curl -X POST http://127.0.0.1:5088/quotes/marine \
  -H "Content-Type: application/json" \
  -d '{"insuredValue":100000,"territory":"ATLANTIC","claimsInLastFiveYears":0}'
```

The sample sets '<PublishAot>false</PublishAot>' and '<PublishTrimmed>false</PublishTrimmed>'. It does not require external services or credentials.

## Testing plugin compatibility

Contract tests should load each plugin against the supported host contract. Host tests should load deliberately incompatible plugins, a duplicate capability, a missing dependency, a newer contract requirement, and two plugins that use conflicting private package versions. The last case is the one that proves separate load contexts are doing useful work.

## Security and trust boundaries

Plugins execute with the host process identity, memory and permissions. 'AssemblyLoadContext' is **not** a sandbox and **not** a reliability boundary. A faulty plugin can throw unhandled exceptions, exhaust memory, block thread-pool threads or use any credential available to the host.

Microsoft documents that untrusted code cannot be loaded safely into a trusted .NET process. Untrusted plugins require process or container isolation, with communication over a serialised contract.

## Lifecycle and restart-based upgrades

Load contexts are collectible, matching the .NET plugin guidance, but version 1 does not expose or promise runtime unloading. Once plugin implementation types are registered in the root DI container, the service provider retains references to those types and prevents meaningful live unloading.

Replace a plugin by publishing a new directory, deploying a new host instance, verifying it, and retiring the old instance. Do not patch files in a running process.

Version 1 does not implement hot swapping, file-system watching, remote downloads, a marketplace, dynamic installation, process isolation, a sandbox, Native AOT or trimming.

## Trimming and Native AOT limitations

Native AOT does not support dynamic assembly loading. Trimming cannot statically discover plugin types found through reflection. 'AddModuleDock' is marked with 'RequiresUnreferencedCode' and 'RequiresDynamicCode'. ModuleDock is unsupported on trimmed or Native AOT hosts.

## Comparison with existing approaches

| Approach | What it provides | What it is for |
| --- | --- | --- |
| ['AssemblyLoadContext'](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext) and ['AssemblyDependencyResolver'](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support) | Loading primitives | Building a host of your own |
| [McMaster.NETCore.Plugins](https://github.com/natemcmaster/DotNetCorePlugins) | Dynamic assembly loading, dependency isolation and type sharing | Applications that want a loader library around those primitives |
| [Prise](https://github.com/prise-plugin/Prise) | A broad, highly customisable plugin framework | Applications that want a large plugin platform |
| ModuleDock | Manifest-driven startup composition, DI registration, compatibility validation, collision detection, diagnostics and restart-based deployment | Generic Host applications with trusted, directory-deployed plugins |

ModuleDock is not universally better, faster, safer or more isolated than these options. The difference is a deliberate product boundary: startup composition for Generic Host applications, not a general-purpose plugin platform.

## Design principles

- Keep the public API small; keep loaders, path validators and resolvers internal.
- Discover plugins from manifests, not from every DLL on disk.
- Fail in a structured, deterministic way.
- Share only the assemblies that must have host type identity.
- Treat plugins as trusted application code.
- Prefer restarting the process over in-process unloading.

The architecture remains faithful to the [original article](https://dotnetdigest.com/designing-a-plugin-architecture-in-net-10), generalised into a reusable library. Application-specific types stay in the host contract.

## Roadmap

Possible later work, not promised for version 1:

- optional child-container isolation as a path toward draining and replacement
- richer health reporting for skipped plugins
- allow-listed artifact signatures beyond SHA-256 checksums

Not in scope for this library's positioning: hot swapping, a plugin marketplace, remote installation, a sandbox, or Native AOT.

## Local development

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet pack --configuration Release --no-build
dotnet format --verify-no-changes
```

```bash
dotnet run --project samples/QuoteHost --urls http://127.0.0.1:5088
```

## Publishing

See [PUBLISHING.md](PUBLISHING.md) for NuGet Trusted Publishing setup. Releases are created from version tags such as 'v0.1.0'. Do not store a long-lived 'NUGET_API_KEY'.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). This project follows the [Code of Conduct](CODE_OF_CONDUCT.md).

## Licence

MIT. See [LICENSE](LICENSE).
