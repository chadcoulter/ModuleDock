using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModuleDock;
using QuoteHost.Contracts;

namespace MarineRating.Plugin;

public sealed class MarineRatingPlugin : IPluginModule
{
    public void ConfigureServices(IServiceCollection services, PluginContext context)
    {
        services.AddKeyedScoped<IRiskCalculator, MarineRiskCalculator>("marine");
    }
}

internal sealed class MarineRiskCalculator(
    ILogger<MarineRiskCalculator> logger) : IRiskCalculator
{
    public ValueTask<QuoteResult> CalculateAsync(
        QuoteRequest request,
        CancellationToken stopToken)
    {
        stopToken.ThrowIfCancellationRequested();

        var territoryFactor = request.Territory.ToUpperInvariant() switch
        {
            "ATLANTIC" => 1.18m,
            "MEDITERRANEAN" => 1.08m,
            _ => 1.12m
        };

        var claimsFactor = 1m + (request.ClaimsInLastFiveYears * 0.04m);
        var premium = decimal.Round(
            request.InsuredValue * 0.0065m * territoryFactor * claimsFactor,
            2,
            MidpointRounding.AwayFromZero);

        logger.LogInformation(
            "Marine quote calculated for territory {Territory}",
            request.Territory);

        return ValueTask.FromResult(
            new QuoteResult("marine", premium, "marine-rating"));
    }
}
