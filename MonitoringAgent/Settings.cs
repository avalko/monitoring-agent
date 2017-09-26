using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitoringAgent
{
    public class Settings
    {
        public int AgentPort { get; set; } = 5000;
        public bool LoggingEnabled { get; set; } = true;
        public string LoggingDirectory { get; set; } = "logs";
        public bool DaemonMode { get; set; } = false;
        public int SaveHistorySeconds { get; set; } = 2678400;
        public int AutoSaveHistorySeconds { get; set; } = 10;
        public bool AutoSaveHistoryEnabled { get; set; } = true;
        public int MaxReturnHistoryItems { get; set; } = 100;
        public string TokenString { get; set; }

        public bool IsCorrect()
        {
            return AgentPort > 0 &&
                   !string.IsNullOrWhiteSpace(LoggingDirectory) &&
                   !string.IsNullOrWhiteSpace(TokenString);
        }

        public Settings()
        {
            TokenString = Guid.NewGuid().ToByteArray().Select(x => x.ToString("X2")).JoinString();
        }
    }
}
