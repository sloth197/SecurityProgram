using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SecurityProgram.App.Models;

namespace SecurityProgram.App.Core.Monitoring;

public class EventLogMonitorService
{
    private EventLog? _eventLog;

    public event Action<EventLogItem>? OnEventReceived;

    public HashSet<EventLogEntryType> AllowedTypes { get; } = new()
    {
        EventLogEntryType.Error,
        EventLogEntryType.Warning,
        EventLogEntryType.FailureAudit,
    };

    public HashSet<int> AllowedEventIds { get; } = new() { 4625 };

    public void Start(string logName = "Security")
    {
        Stop();

        _eventLog = new EventLog(logName)
        {
            EnableRaisingEvents = true,
        };

        _eventLog.EntryWritten += OnEntryWritten;
    }

    public void Stop()
    {
        if (_eventLog is null)
        {
            return;
        }

        _eventLog.EntryWritten -= OnEntryWritten;
        _eventLog.EnableRaisingEvents = false;
        _eventLog.Dispose();
        _eventLog = null;
    }

    private void OnEntryWritten(object sender, EntryWrittenEventArgs e)
    {
        var entry = e.Entry;
        if (entry is null)
        {
            return;
        }

        if (AllowedTypes.Count > 0 && !AllowedTypes.Contains(entry.EntryType))
        {
            return;
        }

        var eventId = unchecked((int)entry.InstanceId);

        if (AllowedEventIds.Any() && !AllowedEventIds.Contains(eventId))
        {
            return;
        }

        OnEventReceived?.Invoke(new EventLogItem
        {
            TimeGenerated = entry.TimeGenerated,
            Source = entry.Source,
            EntryType = entry.EntryType.ToString(),
            Message = entry.Message,
            EventId = eventId,
        });
    }
}
