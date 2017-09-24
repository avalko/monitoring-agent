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
        private UpTimeDta _value = new UpTimeDta();

        public override void Init()
        {
            _value = new UpTimeDta();
            _value.Idle = 0;
        }

        public override void Update()
        {
            var line = VirtualFile.ReadLine(VirtualFile.PathToUpTime);
            var items = line.SplitSpaces();
            if (items.Length > 1)
                _value.Idle = double.Parse(items[1], CultureInfo.InvariantCulture);
            _value.Work = double.Parse(items[0], CultureInfo.InvariantCulture);
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_value);
        }

        class UpTimeDta
        {
            public double Work { get; set; }
            public double Idle { get; set; }
        }
    }
}
