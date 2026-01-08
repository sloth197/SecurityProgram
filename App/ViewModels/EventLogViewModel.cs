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
        
        FilterInfo = $"표시 중: login fail(4625)" +
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
        private void OnEventReceived(EventLogItem item)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                EventLogs.Insert(0, item);
                if (item.EventId == 4625)
                {
                    HandlerFailedLogin();
                }
            });
        }
        //display filter status
        private string _filterInfo = "표시: 오류 / 실패 / 경고";
        public string _filterInfo{
            get => _filterInfo;
            set
            {
                _filterInfo = value;
                OnPropertyChanged();
            }
        } 
    }
    //Add Counter, Timer
    private int _failedLoginCount = 0;
    private DateTime _WindowStartTime = DateTime.Now;
    //로그인 실패 누적 5회 ->  5분 제한 및 경고 메세지 
    private const int FailedLoginThreshold =  5;
    private readonly TimeSpan MonitoringWindow = TimeSpan.FromMinutes(5);
    private string _alertMessage;
    public string _alertMessage
    {
        get => _alertMessage;
        set
        {
            _alertMessage = value;
            OnPropertyChanged();
        }
    }
    //accumulate fail handle
    private void HandleFailedLogin()
    {
        var now = DateTime.Now;
        //시간 초과시 초기화
        if (now - _windowStartTime > MonitoringWindow)
        {
            _failedLoginCount = 0;
            _windowStartTime = now;
            AlertMessage = string.Empty;
        }
        _failedLoginCount++;
        if (_failedLoginCount >= FailedLoginThreshold)
        {
            AlertMessage = $" 로그인 실패ㅜ {_failedLoginCount}회 발생 (Brute-force 공격 의심됨.) 5분간 로그인 제한.";
        }
    }
}