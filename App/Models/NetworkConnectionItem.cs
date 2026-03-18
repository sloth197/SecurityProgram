namespace SecurityProgram.App.Models;

public class NetworkConnectionItem
{
    public string Protocol { get; init; } = string.Empty;

    public string LocalAddress { get; init; } = string.Empty;

    public string ForeignAddress { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public int LocalPort { get; init; }

    public string RiskLevel { get; init; } = "Info";
}
