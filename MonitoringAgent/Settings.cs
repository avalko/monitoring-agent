using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    class Settings
    {
        public int AgentPort { get; set; } = 5000;
        public bool LogEnable { get; set; } = true;
        public string LogFile { get; set; } = "main.log";
        public bool DaemonMode { get; set; } = false;
    }
}
