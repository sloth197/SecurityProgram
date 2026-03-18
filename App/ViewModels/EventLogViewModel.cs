using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using SecurityProgram.App.Core.Monitoring;
using SecurityProgram.App.Models;

namespace SecurityProgram.App.ViewModels;

public class EventLogViewModel : ViewModelBase
{
    private readonly EventLogMonitorService _monitorService;

    private bool _showError = true;
    private bool _showWarning = true;
    private bool _showFailure = true;
    private string _filterInfo = "Filtering: Error, Warning, FailureAudit (EventID:4625)";
    private string _alertMessage = string.Empty;

    private int _failedLoginCount;
    private DateTime _windowStartTime = DateTime.Now;

    private const int FailedLoginThreshold = 5;
    private static readonly TimeSpan MonitoringWindow = TimeSpan.FromMinutes(5);

    public ObservableCollection<EventLogItem> Events { get; } = new();

    public bool ShowError
    {
        get => _showError;
        set
        {
            if (SetProperty(ref _showError, value))
            {
                UpdateFilters();
            }
        }
    }

    public bool ShowWarning
    {
        get => _showWarning;
        set
        {
            if (SetProperty(ref _showWarning, value))
            {
                UpdateFilters();
            }
        }
    }

    public bool ShowFailure
    {
        get => _showFailure;
        set
        {
            if (SetProperty(ref _showFailure, value))
            {
                UpdateFilters();
            }
        }
    }

    public string FilterInfo
    {
        get => _filterInfo;
        private set => SetProperty(ref _filterInfo, value);
    }

    public string AlertMessage
    {
        get => _alertMessage;
        private set => SetProperty(ref _alertMessage, value);
    }

    public EventLogViewModel()
    {
        _monitorService = new EventLogMonitorService();
        _monitorService.OnEventReceived += OnEventReceived;

        UpdateFilters();

        try
        {
            _monitorService.Start();
        }
        catch (Exception ex)
        {
            FilterInfo = $"Event monitoring unavailable: {ex.Message}";
        }
    }

    private void UpdateFilters()
    {
        _monitorService.AllowedTypes.Clear();

        if (ShowError)
        {
            _monitorService.AllowedTypes.Add(EventLogEntryType.Error);
        }

        if (ShowWarning)
        {
            _monitorService.AllowedTypes.Add(EventLogEntryType.Warning);
        }

        if (ShowFailure)
        {
            _monitorService.AllowedTypes.Add(EventLogEntryType.FailureAudit);
        }

        var status =
            $"Filtering: {(ShowError ? "Error " : string.Empty)}" +
            $"{(ShowWarning ? "Warning " : string.Empty)}" +
            $"{(ShowFailure ? "FailureAudit" : string.Empty)} (EventID:4625)";

        FilterInfo = status.Trim();
    }

    private void OnEventReceived(EventLogItem item)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        dispatcher.Invoke(() =>
        {
            Events.Insert(0, item);
            if (Events.Count > 500)
            {
                Events.RemoveAt(Events.Count - 1);
            }

            if (item.EventId == 4625)
            {
                HandleFailedLogin();
            }
        });
    }

    private void HandleFailedLogin()
    {
        var now = DateTime.Now;

        if (now - _windowStartTime > MonitoringWindow)
        {
            _failedLoginCount = 0;
            _windowStartTime = now;
            AlertMessage = string.Empty;
        }

        _failedLoginCount++;

        if (_failedLoginCount >= FailedLoginThreshold)
        {
            AlertMessage =
                $"Warning: {_failedLoginCount} failed logins detected within 5 minutes.";
        }
    }
}
