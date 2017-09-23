using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringAgent.Monitors
{
    [Monitor("cpu")]
    class CpuMonitor : BaseMonitor
    {
        public override string PathToFile => "/proc/stat";
        
        private Scanf _scanf;

        class CpuData
        {
            public double LoadPercent { get; set; }

            [JsonIgnore]
            public int LastActive { get; set; }
            [JsonIgnore]
            public int LastIdle { get; set; }
        }

        private CpuData _value = new CpuData();

        private async Task<int> _GetCpuUsage()
        {
            var cpu = await _ReadLineAsync();
            var result = _scanf.Matches(cpu);
            int usage = 0;
            usage += (int)result[1]; // usr
            usage += (int)result[3]; // sys
            usage += (int)result[4]; // idle
            usage += (int)result[5]; // iowait
            return usage;
        }

        public override async void Init()
        {
            // name usr nice sys idle iowait irq
            _scanf = Scanf.Create("%s %i %i %i %i %i %i %i %i %i %i");

            _value = new CpuData();
            var cpu = await _ReadLineAsync();
            var result = _scanf.Matches(cpu);
            _value.LastActive = 0;
            _value.LastActive += (int)result[1]; // usr
            _value.LastActive += (int)result[3]; // sys
            _value.LastActive += (int)result[5]; // iowait
            _value.LastIdle = (int)result[4]; // idle
        }

        public override async void Next()
        {
            var cpu = await _ReadLineAsync();
            var result = _scanf.Matches(cpu);
            int lastActive = (int)result[1] + (int)result[3] + (int)result[5];
            int lastIdle = (int)result[4];

            int active = lastActive - _value.LastActive;
            int idle = lastIdle - _value.LastIdle;
            int total = active + idle;

            _value.LoadPercent = active * 100.0 / total;

            _value.LastActive = lastActive;
            _value.LastIdle = lastIdle;
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_value);
        }
    }
}
