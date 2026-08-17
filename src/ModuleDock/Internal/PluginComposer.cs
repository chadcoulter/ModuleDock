using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModuleDock;

namespace ModuleDock.Internal;

internal static class PluginComposer
{
    private const string _manifestFileName = "plugin.json";

    [RequiresUnreferencedCode(TrimmingMessages.Discovery)]
    [RequiresDynamicCode(TrimmingMessages.Discovery)]
    public static IPluginCatalog AddModuleDock(IServiceCollection services, ModuleDockOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IPluginCatalog)))
        {
            throw new InvalidOperationException("ModuleDock has already been added to this service collection.");
        }

        var pluginRoot = Path.GetFullPath(options.PluginDirectory);
        var sharedAssemblies = SharedAssemblyNames.Create(options);
        var diagnostics = new List<PluginDiagnostic>();
        var innerExceptions = new List<Exception>();
        var candidates = new List<LoadedCandidate>();

        if (!Directory.Exists(pluginRoot))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingPluginDirectory,
                "The configured plugin directory does not exist.",
                pluginPath: pluginRoot));
        }
        else if (TryEnumeratePluginDirectories(pluginRoot, diagnostics, innerExceptions, out var directories))
        {
            foreach (var directory in directories)
            {
                if (TryCreateCandidate(
                    directory,
                    pluginRoot,
                    options,
                    sharedAssemblies,
                    diagnostics,
                    innerExceptions,
                    out var candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        var accepted = ExcludeDuplicates(candidates, diagnostics);
        var loaded = new List<PluginDescriptor>(accepted.Count);
        var registrationSnapshot = services.ToArray();

        foreach (var candidate in accepted)
        {
            var pluginSnapshot = services.ToArray();
            try
            {
                candidate.Module.ConfigureServices(services, candidate.Context);
                loaded.Add(candidate.Descriptor);
            }
            catch (Exception exception)
            {
                innerExceptions.Add(Unwrap(exception));
                Restore(services, pluginSnapshot);
                diagnostics.Add(new PluginDiagnostic(
                    PluginDiagnosticCode.ServiceRegistrationFailed,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The plugin threw {0} while registering services.",
                        exception.GetType().Name),
                    candidate.Descriptor.Id,
                    candidate.Descriptor.Directory));
            }
        }

        var report = new PluginLoadReport(
            pluginRoot,
            options.FailurePolicy,
            options.ContractVersion,
            loaded,
            diagnostics);

        if (options.FailurePolicy == PluginFailurePolicy.FailFast && diagnostics.Count > 0)
        {
            Restore(services, registrationSnapshot);
            throw CreateValidationException(diagnostics, report, innerExceptions);
        }

        var catalog = new PluginCatalog(report);
        services.AddSingleton<IPluginCatalog>(catalog);
        services.AddSingleton(report);
        return catalog;
    }

    internal static void ValidateOptions(ModuleDockOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PluginDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ContractVersion);

        if (!PluginVersion.TryParse(options.ContractVersion, out _))
        {
            throw new ArgumentException(
                "ContractVersion must be a valid version such as 1.0.0.",
                nameof(options));
        }

        if (!Enum.IsDefined(options.FailurePolicy))
        {
            throw new ArgumentException(
                "FailurePolicy must be FailFast or SkipInvalid.",
                nameof(options));
        }
    }

    private static bool TryEnumeratePluginDirectories(
        string pluginRoot,
        List<PluginDiagnostic> diagnostics,
        List<Exception> innerExceptions,
        [NotNullWhen(true)] out IReadOnlyList<string>? directories)
    {
        try
        {
            directories = [.. Directory.GetDirectories(pluginRoot)
                .Where(static directory =>
                {
                    var name = Path.GetFileName(directory);
                    return !string.IsNullOrEmpty(name) && !name.StartsWith('.');
                })
                .OrderBy(static directory => Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase)
                .ThenBy(static directory => Path.GetFileName(directory), StringComparer.Ordinal)];
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            innerExceptions.Add(exception);
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.PluginDirectoryUnreadable,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The plugin directory could not be read ({0}).",
                    exception.GetType().Name),
                pluginPath: pluginRoot));
            directories = null;
            return false;
        }
    }

    [RequiresUnreferencedCode(TrimmingMessages.Discovery)]
    [RequiresDynamicCode(TrimmingMessages.Discovery)]
    private static bool TryCreateCandidate(
        string pluginDirectory,
        string pluginRoot,
        ModuleDockOptions options,
        IReadOnlySet<string> sharedAssemblies,
        List<PluginDiagnostic> diagnostics,
        List<Exception> innerExceptions,
        [NotNullWhen(true)] out LoadedCandidate? candidate)
    {
        candidate = null;
        string? manifestPath;
        try
        {
            manifestPath = FindManifestPath(pluginDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            innerExceptions.Add(exception);
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.PluginDirectoryUnreadable,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The plugin directory could not be read ({0}).",
                    exception.GetType().Name),
                pluginPath: pluginDirectory));
            return false;
        }

        if (manifestPath is null)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingManifest,
                "The plugin directory does not contain plugin.json.",
                pluginPath: pluginDirectory));
            return false;
        }

        if (!PluginManifestReader.TryRead(manifestPath, out var dto, out var readDiagnostic))
        {
            diagnostics.Add(readDiagnostic!);
            return false;
        }

        if (!TryValidateManifest(dto!, pluginDirectory, options, diagnostics, out var manifest, out var capabilities))
        {
            return false;
        }

        if (!TryResolveEntryAssembly(manifest, pluginDirectory, pluginRoot, diagnostics, out var entryPath))
        {
            return false;
        }

        var depsPath = Path.ChangeExtension(entryPath, ".deps.json");
        var hasDeps = File.Exists(depsPath);

        if (options.RequireDependencyMetadata && !hasDeps)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingDependencyMetadata,
                "The plugin does not include a .deps.json file next to its entry assembly.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (hasDeps && !PluginDependencyMetadata.IsUsable(depsPath, out var depsReason))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MalformedDependencyMetadata,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The plugin's .deps.json {0}.",
                    depsReason),
                manifest.Id,
                pluginDirectory));
            return false;
        }

        PluginDiagnostic? checksumDiagnostic;
        try
        {
            checksumDiagnostic = PluginChecksum.Validate(
                entryPath,
                manifest.ChecksumSha256,
                manifest.Id,
                pluginDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            innerExceptions.Add(exception);
            checksumDiagnostic = new PluginDiagnostic(
                PluginDiagnosticCode.PluginDirectoryUnreadable,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The entry assembly could not be read for checksum validation ({0}).",
                    exception.GetType().Name),
                manifest.Id,
                pluginDirectory);
        }

        if (checksumDiagnostic is not null)
        {
            diagnostics.Add(checksumDiagnostic);
            return false;
        }

        PluginLoadContext loadContext;
        Assembly assembly;
        try
        {
            loadContext = new PluginLoadContext(entryPath, sharedAssemblies);
            assembly = loadContext.LoadFromAssemblyPath(entryPath);
        }
        catch (Exception exception)
        {
            innerExceptions.Add(Unwrap(exception));
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.PluginLoadFailed,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The entry assembly could not be loaded ({0}).",
                    Unwrap(exception).GetType().Name),
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (!TryCreateModule(assembly, manifest, pluginDirectory, diagnostics, innerExceptions, out var module))
        {
            return false;
        }

        var descriptor = new PluginDescriptor(
            manifest.Id,
            manifest.Version,
            manifest.ContractVersion,
            capabilities,
            pluginDirectory,
            Path.GetRelativePath(pluginDirectory, entryPath));

        var context = new PluginContext(
            manifest.Id,
            manifest.Version,
            manifest.ContractVersion,
            capabilities,
            pluginDirectory);

        candidate = new LoadedCandidate(descriptor, module, context, loadContext);
        return true;
    }

    private static string? FindManifestPath(string pluginDirectory)
    {
        foreach (var file in Directory.GetFiles(pluginDirectory))
        {
            if (string.Equals(Path.GetFileName(file), _manifestFileName, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    private static bool TryValidateManifest(
        PluginManifestDto dto,
        string pluginDirectory,
        ModuleDockOptions options,
        List<PluginDiagnostic> diagnostics,
        [NotNullWhen(true)] out PluginManifest? manifest,
        [NotNullWhen(true)] out IReadOnlySet<string>? capabilities)
    {
        manifest = null;
        capabilities = null;
        var pluginId = dto.Id?.Trim();

        if (string.IsNullOrWhiteSpace(dto.Id)
            || string.IsNullOrWhiteSpace(dto.Version)
            || string.IsNullOrWhiteSpace(dto.EntryAssembly)
            || string.IsNullOrWhiteSpace(dto.ContractVersion))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingRequiredField,
                "The plugin manifest must include id, version, entryAssembly and contractVersion.",
                pluginId,
                pluginDirectory));
            return false;
        }

        if (!PluginIdValidator.IsValid(dto.Id))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.InvalidPluginId,
                "Plugin identifiers must start with a letter or digit and may contain letters, digits, '.', '_' or '-'.",
                pluginId,
                pluginDirectory));
            return false;
        }

        if (!PluginVersion.TryParse(dto.Version, out _))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.InvalidVersion,
                "The plugin version is not a valid version value.",
                pluginId,
                pluginDirectory));
            return false;
        }

        if (!PluginVersion.TryParse(dto.ContractVersion, out _))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.InvalidVersion,
                "The contract version is not a valid version value.",
                pluginId,
                pluginDirectory));
            return false;
        }

        if (PluginVersion.Compare(dto.ContractVersion, options.ContractVersion) > 0)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.ContractMismatch,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The plugin requires contract {0}, but the host provides {1}.",
                    dto.ContractVersion.Trim(),
                    options.ContractVersion),
                pluginId,
                pluginDirectory));
            return false;
        }

        var uniqueCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (dto.Capabilities is not null)
        {
            foreach (var capability in dto.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(capability))
                {
                    diagnostics.Add(new PluginDiagnostic(
                        PluginDiagnosticCode.MissingRequiredField,
                        "Plugin capabilities must be non-empty strings.",
                        pluginId,
                        pluginDirectory));
                    return false;
                }

                if (!uniqueCapabilities.Add(capability.Trim()))
                {
                    diagnostics.Add(new PluginDiagnostic(
                        PluginDiagnosticCode.DuplicateCapabilityInManifest,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The plugin manifest lists capability '{0}' more than once.",
                            capability.Trim()),
                        pluginId,
                        pluginDirectory,
                        capability.Trim()));
                    return false;
                }
            }
        }

        manifest = new PluginManifest(
            dto.Id.Trim(),
            dto.Version.Trim(),
            dto.EntryAssembly.Trim(),
            dto.ContractVersion.Trim(),
            [.. uniqueCapabilities],
            string.IsNullOrWhiteSpace(dto.EffectiveChecksum) ? null : dto.EffectiveChecksum.Trim());

        capabilities = uniqueCapabilities.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        return true;
    }

    private static bool TryResolveEntryAssembly(
        PluginManifest manifest,
        string pluginDirectory,
        string pluginRoot,
        List<PluginDiagnostic> diagnostics,
        [NotNullWhen(true)] out string? entryPath)
    {
        entryPath = null;

        if (Path.IsPathRooted(manifest.EntryAssembly))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.RootedEntryPath,
                "The entry assembly path must be relative to the plugin directory.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (PluginPathValidator.ContainsTraversal(manifest.EntryAssembly))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.PathTraversal,
                "The entry assembly path must not contain '..' segments.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        string resolved;
        try
        {
            resolved = Path.GetFullPath(Path.Combine(pluginDirectory, manifest.EntryAssembly));
        }
        catch (ArgumentException)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingEntryAssembly,
                "The entry assembly path contains characters that are not valid in a path.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (!PluginPathValidator.IsInsideDirectory(resolved, pluginDirectory)
            || !PluginPathValidator.IsInsideDirectory(resolved, pluginRoot))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.EntryAssemblyOutsidePluginDirectory,
                "The entry assembly resolves outside the plugin directory.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (!File.Exists(resolved))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MissingEntryAssembly,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The entry assembly '{0}' was not found.",
                    Path.GetFileName(resolved)),
                manifest.Id,
                pluginDirectory));
            return false;
        }

        entryPath = resolved;
        return true;
    }

    [RequiresUnreferencedCode(TrimmingMessages.Discovery)]
    [RequiresDynamicCode(TrimmingMessages.Discovery)]
    private static bool TryCreateModule(
        Assembly assembly,
        PluginManifest manifest,
        string pluginDirectory,
        List<PluginDiagnostic> diagnostics,
        List<Exception> innerExceptions,
        [NotNullWhen(true)] out IPluginModule? module)
    {
        module = null;
        Type[] moduleTypes;
        try
        {
            moduleTypes = [.. assembly.ExportedTypes.Where(static type =>
                type is { IsAbstract: false, IsInterface: false }
                && typeof(IPluginModule).IsAssignableFrom(type))];
        }
        catch (Exception exception)
        {
            innerExceptions.Add(Unwrap(exception));
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.PluginLoadFailed,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Plugin types could not be inspected ({0}).",
                    Unwrap(exception).GetType().Name),
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (moduleTypes.Length == 0)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.NoPluginModule,
                "The entry assembly must expose exactly one concrete IPluginModule.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        if (moduleTypes.Length > 1)
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.MultiplePluginModules,
                "The entry assembly must expose exactly one concrete IPluginModule.",
                manifest.Id,
                pluginDirectory));
            return false;
        }

        try
        {
            var instance = Activator.CreateInstance(moduleTypes[0]);
            module = instance as IPluginModule;
            if (module is null)
            {
                diagnostics.Add(new PluginDiagnostic(
                    PluginDiagnosticCode.ModuleConstructionFailed,
                    "The plugin module could not be created as IPluginModule. Share ModuleDock.Abstractions with the plugin.",
                    manifest.Id,
                    pluginDirectory));
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            innerExceptions.Add(Unwrap(exception));
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticCode.ModuleConstructionFailed,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The plugin module could not be constructed ({0}).",
                    Unwrap(exception).GetType().Name),
                manifest.Id,
                pluginDirectory));
            return false;
        }
    }

    private static List<LoadedCandidate> ExcludeDuplicates(
        List<LoadedCandidate> candidates,
        List<PluginDiagnostic> diagnostics)
    {
        var accepted = new List<LoadedCandidate>(candidates.Count);
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var skip = false;
            if (seenIds.Contains(candidate.Descriptor.Id))
            {
                diagnostics.Add(new PluginDiagnostic(
                    PluginDiagnosticCode.DuplicatePluginId,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Plugin id '{0}' is already registered.",
                        candidate.Descriptor.Id),
                    candidate.Descriptor.Id,
                    candidate.Descriptor.Directory));
                skip = true;
            }

            foreach (var capability in candidate.Descriptor.Capabilities)
            {
                if (seenCapabilities.Contains(capability))
                {
                    diagnostics.Add(new PluginDiagnostic(
                        PluginDiagnosticCode.DuplicateCapability,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Capability '{0}' is already registered by another plugin.",
                            capability),
                        candidate.Descriptor.Id,
                        candidate.Descriptor.Directory,
                        capability));
                    skip = true;
                }
            }

            if (skip)
            {
                continue;
            }

            seenIds.Add(candidate.Descriptor.Id);
            foreach (var capability in candidate.Descriptor.Capabilities)
            {
                seenCapabilities.Add(capability);
            }

            accepted.Add(candidate);
        }

        return accepted;
    }

    private static void Restore(IServiceCollection services, ServiceDescriptor[] snapshot)
    {
        // A plugin may replace or remove host registrations before it throws, so the
        // whole collection is restored rather than only the descriptors it appended.
        services.Clear();
        foreach (var descriptor in snapshot)
        {
            services.Add(descriptor);
        }
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: { } inner }
            ? inner
            : exception;

    private static PluginValidationException CreateValidationException(
        IReadOnlyList<PluginDiagnostic> diagnostics,
        PluginLoadReport report,
        IReadOnlyList<Exception> innerExceptions)
    {
        Exception? inner = innerExceptions.Count switch
        {
            0 => null,
            1 => innerExceptions[0],
            _ => new AggregateException(innerExceptions)
        };

        return new PluginValidationException(
            "Plugin loading failed.",
            diagnostics,
            report,
            inner);
    }

    internal sealed record LoadedCandidate(
        PluginDescriptor Descriptor,
        IPluginModule Module,
        PluginContext Context,
        PluginLoadContext LoadContext);
}
