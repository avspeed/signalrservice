using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCSignalingService.Logging
{
    public class Logger
    {
        private string Application;
        private string EventLogName;

        public enum logType
        {
            info  = EventLogEntryType.Information,
            error = EventLogEntryType.Error,
            warn = EventLogEntryType.Warning
        }

        public Logger(string app, string log)
        {
            Application = app;
            EventLogName = log;

            // Create the event log if it doesn't exist
            if (!EventLog.SourceExists(Application))
            {
                EventLog.CreateEventSource(Application, EventLogName);
            }

        }

        public void WriteToEventLog(string message, logType type)
        {
            EventLog.WriteEntry(Application, message, (EventLogEntryType)type);
        }
    }
}
