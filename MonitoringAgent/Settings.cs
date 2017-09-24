﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    class Settings
    {
        public int AgentPort { get; set; } = 5000;
        public bool LogEnable { get; set; } = true;
        public string LogDir { get; set; } = "logs";
        public bool DaemonMode { get; set; } = false;
        public int SaveHistory { get; set; } = 2678400;
    }
}