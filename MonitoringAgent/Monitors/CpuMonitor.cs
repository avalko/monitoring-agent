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
        private int _lastActive, _lastIdle;

        public override void Init()
        {            
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
            var result = cpu.SplitSpaces();

            // skip name
            // user + nice + system + idle (+ iowait + irq + softirq + steal + guest + guest nice)
            active = result.Skip(1).Select(x => int.Parse(x)).Sum();
            idle = int.Parse(result[4]);
            // compute only active cpu usage
            active -= idle;
        }
    }
}
