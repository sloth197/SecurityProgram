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
        private void EventLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            var entry = e.Entry;
            OnEventReceived?.Invoke(new EventLogItem
            {
                TimeGenerated = entry.TimeGenerted,
                Source = entry.Source,
                EntryType = entry.EntryType.ToString(),
                Message = entry.Message
            });
        }
    }
}