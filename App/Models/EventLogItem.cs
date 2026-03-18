using System;

namespace SecurityProgram.App.Models;

public class EventLogItem
{
    public DateTime TimeGenerated { get; set; }

    public string Source { get; set; } = string.Empty;

    public string EntryType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public int EventId { get; set; }
}
