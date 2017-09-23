using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    interface IMonitor
    {
        string PathToFile { get; }
        void Init();
        string GetJson();
        void Next();
    }
}
