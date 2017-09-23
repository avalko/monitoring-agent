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
        private CpuData _value = new CpuData();

        private void _GetCpuUsage(out int active, out int idle)
        {
            var cpu = VirtualFile.ReadLine(VirtualFile.PathToProcStat);
            var result = _scanf.Matches(cpu);

            // skip name
            active = result.Skip(1).Select(x => (int)x).Sum();
            idle = (int)result[4];
            active -= idle;
        }

        public override void Init()
        {
            /**
             * name usr nice sys idle iowait irq ...
             **/
            _scanf = Scanf.Create("%s %i %i %i %i %i %i %i %i %i %i");

            _value = new CpuData();
            _GetCpuUsage(out int lastActive, out int lastIdle);
            _value.LastActive = lastActive;
            _value.LastIdle = lastIdle;
        }

        public override void Update()
        {
            _GetCpuUsage(out int lastActive, out int lastIdle);

            int active = lastActive - _value.LastActive;
            int idle = lastIdle - _value.LastIdle;
            int total = active + idle;

            _value.LoadPercent = active * 100.0 / total;
            if (_value.LoadPercent > _value.MaxLoadPercent)
                _value.MaxLoadPercent = _value.LoadPercent;

            _value.LastActive = lastActive;
            _value.LastIdle = lastIdle;
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_value);
        }

        class CpuData
        {
            public double LoadPercent { get; set; }
            public double MaxLoadPercent { get; set; }

            [JsonIgnore]
            public int LastActive { get; set; }
            [JsonIgnore]
            public int LastIdle { get; set; }
        }
    }
}
