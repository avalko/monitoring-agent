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

            active = (int)result[1] +
                     (int)result[2] +
                     (int)result[3] +
                     (int)result[5] +
                     (int)result[6];
            idle = (int)result[4];
        }

        public override void Init()
        {
            /**
             * name usr nice sys idle iowait irq ...
             **/
            _scanf = Scanf.Create("%s %i %i %i %i %i %i %i %i %i %i");

            _value = new CpuData();
            _GetCpuUsage(out int lastActive, out int lastIdle);
            _value.LastRealActive = _value.LastActive = lastActive;
            _value.LastRealActive = _value.LastIdle = lastIdle;
        }

        public override void Next()
        {
            _GetCpuUsage(out int lastActive, out int lastIdle);

            int active = lastActive - _value.LastActive;
            int idle = lastIdle - _value.LastIdle;
            int total = active + idle;

            _value.LoadPercent = active * 100.0 / total;

            _value.LastRealActive = lastActive;
            _value.LastRealIdle = lastIdle;
        }

        public override void Update()
        {
            _value.LastActive = _value.LastRealActive;
            _value.LastIdle = _value.LastRealIdle;
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_value);
        }

        class CpuData
        {
            public double LoadPercent { get; set; }

            [JsonIgnore]
            public int LastActive { get; set; }
            [JsonIgnore]
            public int LastIdle { get; set; }
            [JsonIgnore]
            public int LastRealActive { get; set; }
            [JsonIgnore]
            public int LastRealIdle { get; set; }
        }
    }
}
