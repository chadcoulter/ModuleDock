using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace ModuleDock;

/// <summary>
/// Startup entry point implemented by a plugin.
/// </summary>
/// <remarks>
/// ModuleDock constructs the module with a public parameterless constructor before the
/// host service provider is built. Register services here; do not capture the
/// <see cref="IServiceCollection"/> beyond this method.
/// </remarks>
/// <example>
/// <code>
/// public sealed class MarineRatingPlugin : IPluginModule
/// {
///     public void ConfigureServices(IServiceCollection services, PluginContext context)
///     {
///         services.AddKeyedScoped&lt;IRiskCalculator, MarineRiskCalculator&gt;("marine");
///     }
/// }
/// </code>
/// </example>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public interface IPluginModule
{
    /// <summary>
    /// Registers the plugin's services into the host application collection.
    /// </summary>
    /// <param name="services">The host service collection, still mutable because the provider has not been built.</param>
    /// <param name="context">Identity and capability information for this plugin.</param>
    public void ConfigureServices(IServiceCollection services, PluginContext context);
}
