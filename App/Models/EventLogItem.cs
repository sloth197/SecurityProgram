using System;

namespace SecurityProgram.App.Models
{
    public class EventLogItem
    {
        public DateTime TimeGenerated { get; set; }
        public string Source { get; set; }
        public string EntryType { get; set; }
        public string Message { get; set; }
        public int EventId{ get; set; }
    }
}