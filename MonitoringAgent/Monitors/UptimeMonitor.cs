using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor("uptime")]
    class UptimeMonitor : BaseMonitor
    {
        public override void Init()
        {
            Json.Idle = 0.0D;
            Json.Work = 0.0D;
        }

        public override void Update()
        {
            var line = VirtualFile.ReadLine(VirtualFile.PathToUpTime);
            var items = line.SplitSpaces();
            if (items.Length > 1)
                Json.Idle = double.Parse(items[1], CultureInfo.InvariantCulture);
            Json.Work = double.Parse(items[0], CultureInfo.InvariantCulture);
        }
    }
}
