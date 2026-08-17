namespace ModuleDock.TestContracts;

public interface ITestCalculator
{
    public string PluginId { get; }

    public string GetPrivateDependencyVersion();
}
