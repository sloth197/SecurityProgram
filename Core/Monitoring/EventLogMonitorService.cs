using System.Diagnostics;
using SecurityProgram.App.Models;

namespace SecurityProgram.Core.Monitoring
{
    public class EventLogMonitorService
    {
        private EventLog _eventlog;
        public event Action<EventLogItem> OnEventReceived;

        //Monitoring Start
        public void Start(String logName = "Security")
        {
            _eventlog = new EventLog(logName);
            _eventlog.EntryWritten += EventLog_EntryWritten;
            _eventlog.EnableRaisingEvents = true;
        }
        //Monitoring Stop
        public void Stop()
        {
            if (_eventlog == null)
                return;
            _eventlog.EntryWritten -= EventLog_EntryWritten;
            _eventlog.EnableRaisingEvents = false;
        }
        //EventLog written -> send a message  
        private void EventLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            if (!AllowedTypes.Contains(e.Entry.EntryType))
                return;
            var entry = e.Entry;
            //Modify filter logic
            if (AllowedEventIds.Any() && ! AllowedEventIds.Contains(entry.InstanceId > int.MaxValue ? unchecked((int)entry.InstanceId) : (int)entry.InstanceId))
                return;

            OnEventReceived?.Invoke(new EventLogItem
            {
                TimeGenerated = entry.TimeGenerted,
                Source = entry.Source,
                EntryType = entry.EntryType.ToString(),
                Message = entry.Message,
                //InstanceId : long -> int
                EventId = (int)entry.InstanceId
            });
        }
        //허락(특정의)된 이벤트로그의 타입이 화면에 표시
        public HashSet<EventLogEntryType> _allowedTypes { get; }= new()
        {
            EventLogEntryType.Error,
            EventLogEntryType.Warning,
            EventLogEntryType.FailureAudit
        };
        //Add Filter Collection (Id)
        public HashSet<int> AllowedEventIds { get; } = new()
        {
            4625; //Login fail
        }
    }
}