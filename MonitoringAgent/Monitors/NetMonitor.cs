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
        
        struct PerTimeData
        {
            public int Current { get; set; }
            public int Last { get; set; }
        }

        // RtoAlgorithm RtoMin RtoMax MaxConn (ActiveOpens) (PassiveOpens) (AttemptFails)
        // (EstabResets) CurrEstab* (InSegs) (OutSegs) (RetransSegs) (InErrs) OutRsts InCsumErrors
        private PerTimeData _tcpActiveOpens;
        private PerTimeData _tcpPassiveOpens;
        private PerTimeData _tcpAttemptFails;
        private PerTimeData _tcpEstabResets;
        private PerTimeData _tcpCurrEstab;
        private PerTimeData _tcpInSeg;
        private PerTimeData _tcpOutSeg;
        private PerTimeData _tcpRetransSegs;
        private PerTimeData _tcpInErrs;

        // (InDatagrams) NoPorts (InErrors) (OutDatagrams) (RcvbufErrors) (SndbufErrors) InCsumErrors IgnoredMulti
        private PerTimeData _udpInDatagrams;
        private PerTimeData _udpInErrors;
        private PerTimeData _udpOutDatagrams;
        private PerTimeData _udpRcvbufErrors;
        private PerTimeData _udpSndbufErrors;

        private void _ReadNetStat(Action<int[]> tcp, Action<int[]> udp, Action<int[]> ip)
        {
            var data = VirtualFile.ReadToEnd(VirtualFile.PathToNetSnmp).SplitLines();
            foreach (var line in data)
            {
                var arr = line.Split(':');
                if (arr.Length < 2)
                    continue;

                if (arr[0] == "Tcp" || arr[0] == "Udp" || arr[0] == "Ip")
                {
                    var raw = arr[1].SplitSpaces();
                    if (raw.Length < 2)
                        continue;
                    if (int.TryParse(raw[0], out _))
                    {
                        var numbers = raw.Skip(1).Select(x => int.Parse(x)).ToArray();

                        switch (arr[0].ToLower())
                        {
                            case "tcp":
                                tcp(numbers);
                                break;
                            case "udp":
                                udp(numbers);
                                break;
                            case "ip":
                                ip(numbers);
                                break;
                        }
                    }
                }
            }
        }

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

            void _ReadTcp(int[] numbers)
            {
                _tcpActiveOpens.Last = numbers[4];
                _tcpPassiveOpens.Last = numbers[5];
                _tcpAttemptFails.Last = numbers[6];
                _tcpEstabResets.Last = numbers[7];
                _tcpCurrEstab.Last = numbers[8];
                _tcpInSeg.Last = numbers[9];
                _tcpOutSeg.Last = numbers[10];
                _tcpRetransSegs.Last = numbers[11];
                _tcpInErrs.Last = numbers[12];
            }

            void _ReadUdp(int[] numbers)
            {
                _udpInDatagrams.Last = numbers[0];
                _udpInErrors.Last = numbers[2];
                _udpOutDatagrams.Last = numbers[3];
                _udpRcvbufErrors.Last = numbers[4];
                _udpSndbufErrors.Last = numbers[5];
            }

            void _ReadIp(int[] numbers)
            {
                // TODO
            }

            _ReadNetStat(_ReadTcp, _ReadUdp, _ReadIp);
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

            void _ReadTcp(int[] numbers)
            {
                _tcpActiveOpens.Current = numbers[4] - _tcpActiveOpens.Last;
                _tcpPassiveOpens.Current = numbers[5] - _tcpPassiveOpens.Last;
                _tcpAttemptFails.Current = numbers[6] - _tcpAttemptFails.Last;
                _tcpEstabResets.Current = numbers[7] - _tcpEstabResets.Last;
                _tcpCurrEstab.Current = numbers[8] - _tcpCurrEstab.Last;
                _tcpInSeg.Current = numbers[9] - _tcpInSeg.Last;
                _tcpOutSeg.Current = numbers[10] - _tcpOutSeg.Last;
                _tcpRetransSegs.Current = numbers[11] - _tcpRetransSegs.Last;
                _tcpInErrs.Current = numbers[12] - _tcpInErrs.Last;

                _tcpActiveOpens.Last = numbers[4];
                _tcpPassiveOpens.Last = numbers[5];
                _tcpAttemptFails.Last = numbers[6];
                _tcpEstabResets.Last = numbers[7];
                _tcpCurrEstab.Last = numbers[8];
                _tcpInSeg.Last = numbers[9];
                _tcpOutSeg.Last = numbers[10];
                _tcpRetransSegs.Last = numbers[11];
                _tcpInErrs.Last = numbers[12];
            }

            void _ReadUdp(int[] numbers)
            {
                _udpInDatagrams.Current = numbers[0] - _udpInDatagrams.Last;
                _udpInErrors.Current = numbers[2] - _udpInErrors.Last;
                _udpOutDatagrams.Current = numbers[3] - _udpOutDatagrams.Last;
                _udpRcvbufErrors.Current = numbers[4] - _udpRcvbufErrors.Last;
                _udpSndbufErrors.Current = numbers[5] - _udpSndbufErrors.Last;

                _udpInDatagrams.Last = numbers[0];
                _udpInErrors.Last = numbers[2];
                _udpOutDatagrams.Last = numbers[3];
                _udpRcvbufErrors.Last = numbers[4];
                _udpSndbufErrors.Last = numbers[5];
            }

            void _ReadIp(int[] numbers)
            {
                // TODO
            }

            _ReadNetStat(_ReadTcp, _ReadUdp, _ReadIp);
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(new
            {
                dev = _traffic,
                tcp = new
                {
                    ActiveOpens = _tcpActiveOpens.Current,
                    AttemptFails = _tcpAttemptFails.Current,
                    CurrEstab = _tcpCurrEstab.Current,
                    TotalEstab = _tcpCurrEstab.Last,
                    EstabResets = _tcpEstabResets.Current,
                    InErrs = _tcpInErrs.Current,
                    InSeg = _tcpInSeg.Current,
                    OutSeg = _tcpOutSeg.Current,
                    PassiveOpens = _tcpPassiveOpens.Current,
                    RetransSegs = _tcpRetransSegs.Current,
                },
                udp = new
                {
                    InDatagrams = _udpInDatagrams.Current,
                    InErrors = _udpInErrors.Current,
                    OutDatagrams = _udpOutDatagrams.Current,
                    RcvbufErrors = _udpRcvbufErrors.Current,
                    SndbufErrors = _udpSndbufErrors.Current
                }
            });
        }

        class NetMonitorData
        {
            public double ReceiveBytes { get; set; }
            public double SendBytes { get; set; }
            public int ReceivePackets { get; set; }
            public int SendPackets { get; set; }

            [JsonIgnore]
            public long ReceiveBytesLast { get; set; }
            [JsonIgnore]
            public long SendBytesLast { get; set; }
            [JsonIgnore]
            public int ReceivePacketsLast { get; set; }
            [JsonIgnore]
            public int SendPacketsLast { get; set; }
        }
    }
}
