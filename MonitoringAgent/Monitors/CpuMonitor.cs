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
        private Scanf _scanf;
        private int _lastActive, _lastIdle;

        public override void Init()
        {
            /**
             * name usr nice sys idle iowait irq ...
             **/
            _scanf = Scanf.Create("%s %i %i %i %i %i %i %i %i %i %i");
            
            _GetCpuUsage(out int _lastActive, out int _lastIdle);

            Json.LoadPercent = 0;
            Json.MaxLoadPercent = 0;
        }

        public override void Update()
        {
            _GetCpuUsage(out int currentLastActive, out int currentLastIdle);

            int active = currentLastActive - _lastActive;
            int idle = currentLastIdle - _lastIdle;
            int total = active + idle;

            Json.LoadPercent = active * 100.0 / total;
            if (Json.LoadPercent > Json.MaxLoadPercent)
                Json.MaxLoadPercent = Json.LoadPercent;

            _lastActive = currentLastActive;
            _lastIdle = currentLastIdle;
        }

        private void _GetCpuUsage(out int active, out int idle)
        {
            var cpu = VirtualFile.ReadLine(VirtualFile.PathToProcStat);
            var result = _scanf.Matches(cpu);

            // skip name
            active = result.Skip(1).Select(x => (int)x).Sum();
            idle = (int)result[4];
            active -= idle;
        }
    }
}
