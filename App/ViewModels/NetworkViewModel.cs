using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Models;

namespace SecurityProgram.App.ViewModels;

public class NetworkViewModel : ViewModelBase
{
    private static readonly HashSet<int> HighRiskPorts = new()
    {
        21, 23, 69, 135, 137, 138, 139, 445, 1433, 1521, 3306, 3389, 5432, 5900, 6379,
    };

    private readonly List<NetworkConnectionItem> _allConnections = new();

    private string _filterKeyword = string.Empty;
    private bool _showOnlyRisky;
    private bool _isScanning;
    private string _scanSummary = "Press Refresh to scan local network ports.";

    public ObservableCollection<NetworkConnectionItem> Connections { get; } = new();

    public string FilterKeyword
    {
        get => _filterKeyword;
        set
        {
            if (SetProperty(ref _filterKeyword, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowOnlyRisky
    {
        get => _showOnlyRisky;
        set
        {
            if (SetProperty(ref _showOnlyRisky, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (SetProperty(ref _isScanning, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ScanSummary
    {
        get => _scanSummary;
        private set => SetProperty(ref _scanSummary, value);
    }

    public int TotalCount => Connections.Count;

    public int ListeningCount => Connections.Count(item =>
        string.Equals(item.State, "LISTENING", StringComparison.OrdinalIgnoreCase));

    public int HighRiskCount => Connections.Count(item =>
        string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase));

    public ICommand RefreshCommand { get; }

    public ICommand CopySnapshotCommand { get; }

    public NetworkViewModel()
    {
        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsScanning);
        CopySnapshotCommand = new RelayCommand(_ => CopySnapshot(), _ => Connections.Count > 0);

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsScanning)
        {
            return;
        }

        try
        {
            IsScanning = true;
            ScanSummary = "Scanning active local ports...";

            var snapshot = await Task.Run(ReadConnections);

            _allConnections.Clear();
            _allConnections.AddRange(snapshot
                .OrderByDescending(item => item.RiskLevel == "High")
                .ThenBy(item => item.Protocol)
                .ThenBy(item => item.LocalPort));

            ApplyFilters();

            ScanSummary =
                $"Last scan: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                $"Total: {TotalCount} | Listening: {ListeningCount} | High-risk: {HighRiskCount}";
        }
        catch (Exception ex)
        {
            ScanSummary = $"Network scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<NetworkConnectionItem> query = _allConnections;

        if (ShowOnlyRisky)
        {
            query = query.Where(item =>
                string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterKeyword))
        {
            var keyword = FilterKeyword.Trim();
            query = query.Where(item =>
                item.Protocol.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.LocalAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.ForeignAddress.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.State.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.ProcessId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.LocalPort.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToList();

        Connections.Clear();
        foreach (var item in filtered)
        {
            Connections.Add(item);
        }

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ListeningCount));
        OnPropertyChanged(nameof(HighRiskCount));

        CommandManager.InvalidateRequerySuggested();
    }

    private void CopySnapshot()
    {
        if (Connections.Count == 0)
        {
            ScanSummary = "No scan data to copy yet.";
            return;
        }

        var lines = Connections
            .Take(120)
            .Select(item =>
                $"{item.Protocol,-4} {item.LocalAddress,-24} {item.ForeignAddress,-24} {item.State,-12} PID:{item.ProcessId,-6} {item.ProcessName,-20} Risk:{item.RiskLevel}")
            .ToList();

        var snapshot =
            $"Security Program - Network Snapshot{Environment.NewLine}" +
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
            $"Total: {TotalCount}, Listening: {ListeningCount}, High-risk: {HighRiskCount}{Environment.NewLine}{Environment.NewLine}" +
            string.Join(Environment.NewLine, lines);

        Clipboard.SetText(snapshot);
        ScanSummary = "Network snapshot copied to clipboard.";
    }

    private static List<NetworkConnectionItem> ReadConnections()
    {
        var output = RunProcess("netstat", "-ano");
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var items = new List<NetworkConnectionItem>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("Proto", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Active", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parsed = ParseNetstatLine(line);
            if (parsed is not null)
            {
                items.Add(parsed);
            }
        }

        return items;
    }

    private static NetworkConnectionItem? ParseNetstatLine(string line)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 4)
        {
            return null;
        }

        var protocol = tokens[0].ToUpperInvariant();

        string localAddress;
        string foreignAddress;
        string state;
        string pidText;

        if (protocol == "TCP")
        {
            if (tokens.Length < 5)
            {
                return null;
            }

            localAddress = tokens[1];
            foreignAddress = tokens[2];
            state = tokens[3];
            pidText = tokens[4];
        }
        else if (protocol == "UDP")
        {
            localAddress = tokens[1];
            foreignAddress = tokens[2];
            state = "N/A";
            pidText = tokens[3];
        }
        else
        {
            return null;
        }

        if (!int.TryParse(pidText, out var processId))
        {
            processId = -1;
        }

        var localPort = ParsePort(localAddress);

        return new NetworkConnectionItem
        {
            Protocol = protocol,
            LocalAddress = localAddress,
            ForeignAddress = foreignAddress,
            State = state,
            ProcessId = processId,
            ProcessName = ResolveProcessName(processId),
            LocalPort = localPort,
            RiskLevel = EvaluateRisk(localPort, state),
        };
    }

    private static int ParsePort(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return -1;
        }

        var lastColon = endpoint.LastIndexOf(':');
        if (lastColon < 0 || lastColon >= endpoint.Length - 1)
        {
            return -1;
        }

        var portText = endpoint[(lastColon + 1)..];
        return int.TryParse(portText, out var port) ? port : -1;
    }

    private static string EvaluateRisk(int localPort, string state)
    {
        if (HighRiskPorts.Contains(localPort))
        {
            return "High";
        }

        if (string.Equals(state, "LISTENING", StringComparison.OrdinalIgnoreCase))
        {
            return "Open";
        }

        return "Info";
    }

    private static string ResolveProcessName(int processId)
    {
        if (processId <= 0)
        {
            return "Unknown";
        }

        try
        {
            return Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                ? $"{fileName} exited with code {process.ExitCode}."
                : stderr.Trim());
        }

        return stdout;
    }
}
