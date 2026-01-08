using System.Collections.ObjectModel;
using SecurityProgram.Core.Monitoring;
using SecurityProgram.App.Models;

namespace SecurityProgram.App.ViewModels
{
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