using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    interface IMonitor
    {
        string Tag { get; set; }
        void Init();
        string GetJson();
        void Update();
    }
}
