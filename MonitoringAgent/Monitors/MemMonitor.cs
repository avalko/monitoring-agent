using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor("mem")]
    class MemMonitor  : BaseMonitor
    {
        private MemData _value = new MemData();

        public override void Init()
        {
            _value = new MemData();
        }

        public override void Update()
        {
            var stream = VirtualFile.Open(VirtualFile.PathToMemInfo);

            byte flag = 0b0000000;
            const byte flagMemTotal     = 0b0000001,
                       flagMemFree      = 0b0000010,
                       flagMemAvailable = 0b0000100,
                       flagSwapTotal    = 0b0001000,
                       flagSwapFree     = 0b0010000;
            const byte flagAll = flagMemTotal | flagMemFree | flagMemAvailable | flagSwapTotal | flagSwapFree;

            do
            {
                string[] items = stream.ReadLine().SplitSpaces();

                switch (items[0].TrimEnd(':'))
                {
                    case "MemTotal":
                        _value.Total = int.Parse(items[1]) / 1024.0;
                        flag |= flagMemTotal;
                        break;
                    case "MemFree":
                        _value.Free = int.Parse(items[1]) / 1024.0;
                        flag |= flagMemFree;
                        break;
                    case "MemAvailable":
                        _value.Available = int.Parse(items[1]) / 1024.0;
                        flag |= flagMemAvailable;
                        break;
                    case "SwapTotal":
                        _value.SwapTotal = int.Parse(items[1]) / 1024.0;
                        flag |= flagSwapTotal;
                        break;
                    case "SwapFree":
                        _value.SwapFree = int.Parse(items[1]) / 1024.0;
                        flag |= flagSwapFree;
                        break;
                }
            } while ((flag ^ flagAll) > 0);

            stream.Close();
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_value);
        }

        class MemData
        {
            public double Total { get; set; }
            public double Free { get; set; }
            public double Used => Total - Free;
            public double Available { get; set; }
            public double SwapTotal { get; set; }
            public double SwapFree { get; set; }
        }
    }
}
