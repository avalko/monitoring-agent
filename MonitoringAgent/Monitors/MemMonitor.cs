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

        public override void Init()
        {
            Json.Total = 0;
            Json.Free = 0;
            Json.Used = 0;
            Json.Available = 0;
            Json.SwapTotal = 0;
            Json.SwapFree = 0;
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
                string[] items = stream.ReadLine().Split(':');
                if (items.Length < 2)
                    continue;
                string value = items[1].SplitSpaces()[0];

                switch (items[0])
                {
                    case "MemTotal":
                        Json.Total = int.Parse(value) / 1024.0;
                        flag |= flagMemTotal;
                        break;
                    case "MemFree":
                        Json.Free = int.Parse(value) / 1024.0;
                        flag |= flagMemFree;
                        break;
                    case "MemAvailable":
                        Json.Available = int.Parse(value) / 1024.0;
                        flag |= flagMemAvailable;
                        break;
                    case "SwapTotal":
                        Json.SwapTotal = int.Parse(value) / 1024.0;
                        flag |= flagSwapTotal;
                        break;
                    case "SwapFree":
                        Json.SwapFree = int.Parse(value) / 1024.0;
                        flag |= flagSwapFree;
                        break;
                }
            } while ((flag ^ flagAll) > 0 && !stream.EndOfStream);

            stream.Close();
        }
    }
}
