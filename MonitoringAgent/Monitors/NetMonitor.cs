using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor("net")]
    class NetMonitor : BaseMonitor
    {
        private Dictionary<string, NetMonitorData> _traffic = new Dictionary<string, NetMonitorData>();

        private void _ReadNetDev(Action<string, long[]> rawProcess)
        {
            var stream = VirtualFile.Open(VirtualFile.PathToNetDev);
            //        0     1      2    3   4    5       6          7
            // rec  bytes packets errs drop fifo frame compressed multicast
            //        8      9     10   11   12  13      15       16
            // send bytes packets errs drop fifo colls carrier compressed
            while (!stream.EndOfStream)
            {
                var line = stream.ReadLine();
                var parts = line.Split(':');
                if (parts.Length < 2)
                    continue;

                var data = parts[1].Trim().SplitSpaces();
                var numbers = data.Select(x => long.Parse(x)).ToArray();
                rawProcess(parts[0].Trim(), numbers);
            }
            stream.Close();
        }

        public override void Init()
        {
            _ReadNetDev((name, numbers) =>
            {
                _traffic[name] = new NetMonitorData()
                {
                   ReceiveBytesLast = numbers[0],
                   ReceivePacketsLast = (int)numbers[1],
                   SendBytesLast = numbers[8],
                   SendPacketsLast = (int)numbers[9],
                };
            });
        }

        public override void Update()
        {
            _ReadNetDev((name, numbers) =>
            {
                var traffic = _traffic[name];

                // convert from bytes to KiB
                traffic.ReceiveBytes = (numbers[0] - traffic.ReceiveBytesLast) / 1024.0;
                traffic.ReceivePackets = (int)numbers[1] - traffic.ReceivePacketsLast;
                // convert from bytes to KiB
                traffic.SendBytes = (numbers[8] - traffic.SendBytesLast) / 1024.0;
                traffic.SendPackets = (int)numbers[9] - traffic.SendPacketsLast;

                traffic.ReceiveBytesLast = numbers[0];
                traffic.ReceivePacketsLast = (int)numbers[1];
                traffic.SendBytesLast = numbers[8];
                traffic.SendPacketsLast = (int)numbers[9];
            });
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_traffic);
        }

        class NetMonitorData
        {
            public double ReceiveBytes { get; set; }
            public double SendBytes { get; set; }
            public int ReceivePackets { get; set; }
            public int SendPackets { get; set; }


            public long ReceiveBytesLast { get; set; }
            public long SendBytesLast { get; set; }
            public int ReceivePacketsLast { get; set; }
            public int SendPacketsLast { get; set; }
        }
    }
}
