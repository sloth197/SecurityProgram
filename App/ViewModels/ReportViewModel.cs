using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SecurityProgram.App.Commands;
using SecurityProgram.App.Core.Reporting;

namespace SecurityProgram.App.ViewModels;

public class ReportViewModel : ViewModelBase
{
    private readonly PasswordViewModel _passwordViewModel;
    private readonly NetworkViewModel _networkViewModel;
    private readonly EventLogViewModel _eventLogViewModel;
    private readonly PdfReportService _pdfReportService = new();

    private string _reportTitle = "Security Program - Snapshot Report";
    private string _analystName = Environment.UserName;
    private string _targetSystem = Environment.MachineName;
    private string _executiveSummary = "Summary of password posture, local network exposure, and event log monitoring status.";
    private string _reportPreview = "Generate preview to create a report draft.";
    private string _pdfExportStatus = "PDF export not started.";
    private DateTime? _lastGeneratedAt;

    public string ReportTitle
    {
        get => _reportTitle;
        set => SetProperty(ref _reportTitle, value);
    }

    public string AnalystName
    {
        get => _analystName;
        set => SetProperty(ref _analystName, value);
    }

    public string TargetSystem
    {
        get => _targetSystem;
        set => SetProperty(ref _targetSystem, value);
    }

    public string ExecutiveSummary
    {
        get => _executiveSummary;
        set => SetProperty(ref _executiveSummary, value);
    }

    public string ReportPreview
    {
        get => _reportPreview;
        private set => SetProperty(ref _reportPreview, value);
    }

    public string PdfExportStatus
    {
        get => _pdfExportStatus;
        private set => SetProperty(ref _pdfExportStatus, value);
    }

    public string LastGeneratedText => _lastGeneratedAt is null
        ? "Not generated yet."
        : $"Last generated: {_lastGeneratedAt:yyyy-MM-dd HH:mm:ss}";

    public ICommand GeneratePreviewCommand { get; }

    public ICommand CopyPreviewCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public ReportViewModel(
        PasswordViewModel passwordViewModel,
        NetworkViewModel networkViewModel,
        EventLogViewModel eventLogViewModel)
    {
        _passwordViewModel = passwordViewModel;
        _networkViewModel = networkViewModel;
        _eventLogViewModel = eventLogViewModel;

        GeneratePreviewCommand = new RelayCommand(_ => GeneratePreview());
        CopyPreviewCommand = new RelayCommand(_ => CopyPreview(), _ => !string.IsNullOrWhiteSpace(ReportPreview));
        ExportPdfCommand = new RelayCommand(_ => ExportPdf());
    }

    private void GeneratePreview()
    {
        var highRiskEntries = _networkViewModel.Connections
            .Where(item => string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        var reportTime = DateTime.Now;
        var builder = new StringBuilder();

        builder.AppendLine($"# {ReportTitle}");
        builder.AppendLine();
        builder.AppendLine($"Generated At: {reportTime:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Analyst: {AnalystName}");
        builder.AppendLine($"Target System: {TargetSystem}");
        builder.AppendLine();

        builder.AppendLine("## Executive Summary");
        builder.AppendLine(ExecutiveSummary);
        builder.AppendLine();

        builder.AppendLine("## 1) Password Analysis");
        builder.AppendLine($"- Score: {_passwordViewModel.PasswordScore}/100 ({_passwordViewModel.PasswordLevel})");
        builder.AppendLine($"- Estimated entropy: {_passwordViewModel.EstimatedEntropyBits:F1} bits");
        builder.AppendLine($"- Current guidance: {_passwordViewModel.FeedbackMessage}");
        builder.AppendLine();

        builder.AppendLine("## 2) Network Exposure");
        builder.AppendLine($"- Active entries: {_networkViewModel.TotalCount}");
        builder.AppendLine($"- Listening sockets: {_networkViewModel.ListeningCount}");
        builder.AppendLine($"- High-risk ports: {_networkViewModel.HighRiskCount}");

        if (highRiskEntries.Count == 0)
        {
            builder.AppendLine("- High-risk sample: none detected in current view.");
        }
        else
        {
            builder.AppendLine("- High-risk sample entries:");
            foreach (var entry in highRiskEntries)
            {
                builder.AppendLine($"  - {entry.Protocol} {entry.LocalAddress} PID:{entry.ProcessId} ({entry.ProcessName})");
            }
        }

        builder.AppendLine();

        builder.AppendLine("## 3) Event Log Monitoring");
        builder.AppendLine($"- Captured event count: {_eventLogViewModel.Events.Count}");
        builder.AppendLine($"- Active filter: {_eventLogViewModel.FilterInfo}");

        if (string.IsNullOrWhiteSpace(_eventLogViewModel.AlertMessage))
        {
            builder.AppendLine("- Alert status: no brute-force pattern currently flagged.");
        }
        else
        {
            builder.AppendLine($"- Alert status: {_eventLogViewModel.AlertMessage}");
        }

        builder.AppendLine();

        builder.AppendLine("## 4) Recommended Actions");

        if (_passwordViewModel.PasswordScore < 70)
        {
            builder.AppendLine("- Enforce stronger password policy (minimum 12 chars with mixed character classes).");
        }

        if (_networkViewModel.HighRiskCount > 0)
        {
            builder.AppendLine("- Review exposed high-risk ports and apply firewall hardening where possible.");
        }

        if (!string.IsNullOrWhiteSpace(_eventLogViewModel.AlertMessage))
        {
            builder.AppendLine("- Investigate repeated failed login attempts and review account lockout policy.");
        }

        if (_passwordViewModel.PasswordScore >= 70
            && _networkViewModel.HighRiskCount == 0
            && string.IsNullOrWhiteSpace(_eventLogViewModel.AlertMessage))
        {
            builder.AppendLine("- Current baseline looks stable. Continue periodic monitoring.");
        }

        ReportPreview = builder.ToString().TrimEnd();

        _lastGeneratedAt = reportTime;
        OnPropertyChanged(nameof(LastGeneratedText));
        PdfExportStatus = "Preview generated. Ready to export as PDF.";

        CommandManager.InvalidateRequerySuggested();
    }

    private void CopyPreview()
    {
        if (string.IsNullOrWhiteSpace(ReportPreview))
        {
            return;
        }

        Clipboard.SetText(ReportPreview);
        PdfExportStatus = "Preview copied to clipboard.";
    }

    private void ExportPdf()
    {
        try
        {
            if (_lastGeneratedAt is null || string.IsNullOrWhiteSpace(ReportPreview))
            {
                GeneratePreview();
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save Security Report",
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = $"SecurityReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            };

            if (dialog.ShowDialog() != true)
            {
                PdfExportStatus = "PDF export canceled.";
                return;
            }

            var generatedAt = _lastGeneratedAt ?? DateTime.Now;
            var savedPath = _pdfReportService.ExportReport(
                dialog.FileName,
                ReportTitle,
                AnalystName,
                TargetSystem,
                generatedAt,
                ReportPreview);

            PdfExportStatus = $"PDF export completed: {savedPath}";
        }
        catch (Exception ex)
        {
            PdfExportStatus = $"PDF export failed: {ex.Message}";
        }
    }
}
