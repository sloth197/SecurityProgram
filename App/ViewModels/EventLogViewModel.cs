using System.Collections.ObjectModel;
using SecurityProgram.Core.Monitoring;
using SecurityProgram.App.Models;

namespace SecurityProgram.App.ViewModels
{
    //Add checkbox status
    private bool _showError = true;
    private bool _showWarning = true;
    private bool _showFailure = true;
    public bool _showError
    {
        get => _showError;
        set
        {
            _showError = value;
            UpdateFilters();
            OnPropertyChanged();
        }
    }
    public bool _showWarning
    {
        get => _showWarning;
        set
        {
            _showWarning = value;
            UpdateFilters();
            OnPropertyChanged();
        }
    }
    public bool _showFailure
    {
        get => _showFailure;
        set
        {
            _showFailure = value;
            UpdateFilters();
            OnPropertyChanged();
        }
    }
    //Update filter status
    private void UpdateFilters()
    {
        _monitorService.AllowedTypes.Clear();
        if (_showError)
            _monitorService.AllowedTypes.Add(EventLogEntryType.Error);
        else if (_showWarning)
            _monitorService.AllowedTypes.Add(EventLogEntryType.Warning);
        else if (_showFailure)
            _monitorService.AllowedTypes.Add(EventLogEntryType.FailureAudit);
        FilterInfo = $"표시 중: " +
                     $"{ShowError ? "Error " : ""}" +
                     $"{ShowWarning ? "Warning " : ""}" +
                     $"{ShowFailure ? "Failure " : ""}" ;          
        UpdateFilters();
    }

    public class EventLogViewModel : ViewModelBase
    {
        private readonly EventLogMonitorService _monitorService;
        public ObservableCollection<EventLogItem> Events { get; } = new();
        //Start Monitoring
        public EventLogViewModel()
        {
            _monitorService = new EventLogMonitorService();
            _monitorService.OnEventReceived += AddEvent;
            _monitorService.Start();
        }
        private void AddEvent(EventLogItem item)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Events.Insert(0, item);
                if (Events.Count > 200)
                {
                    Events.RemoveAt(Events.Count - 1);
                }
            });
        }
        //display filter status
        private string _filterInfo = "표시: 오류 / 실패 / 경고";
        public string _filterInfo{
            get => _filterInfo;
            set
            {
                _filterInfor = value;
                OnPropertyChanged();
            }
        } 
    }
}