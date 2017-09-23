using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    interface IMonitor
    {
        void Init();
        string GetJson();
        void Next();
        void Update();
    }
}
