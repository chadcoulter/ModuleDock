using ModuleDock;
using QuoteHost.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddModuleDock(options =>
{
    options.PluginDirectory = Path.Combine(
        builder.Environment.ContentRootPath,
        "plugins");

    options.ContractVersion = "1.0.0";
    options.FailurePolicy = PluginFailurePolicy.FailFast;
    options.ShareAssemblyContaining<IRiskCalculator>();
});

var app = builder.Build();

app.MapGet("/plugins", (IPluginCatalog catalog) =>
    catalog.Plugins.Select(plugin => new
    {
        plugin.Id,
        plugin.Version,
        plugin.ContractVersion,
        Capabilities = plugin.Capabilities
    }));

app.MapPost(
    "/quotes/{productCode}",
    async Task<IResult> (
        string productCode,
        QuoteRequest request,
        IServiceProvider services,
        CancellationToken stopToken) =>
    {
        var calculator = services.GetKeyedService<IRiskCalculator>(
            productCode.ToLowerInvariant());

        if (calculator is null)
        {
            return Results.NotFound(new
            {
                Error = $"No rating plugin supports '{productCode}'."
            });
        }

        var result = await calculator.CalculateAsync(request, stopToken);
        return Results.Ok(result);
    });

app.Run();
