namespace QuoteHost.Contracts;

public sealed record QuoteRequest(
    decimal InsuredValue,
    string Territory,
    int ClaimsInLastFiveYears);

public sealed record QuoteResult(
    string ProductCode,
    decimal Premium,
    string PluginId);

public interface IRiskCalculator
{
    public ValueTask<QuoteResult> CalculateAsync(
        QuoteRequest request,
        CancellationToken stopToken);
}
