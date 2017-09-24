using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor("info")]
    class CpuInfo : BaseMonitor
    {
        private readonly CpuInfoData _value = new CpuInfoData();
        private string _json = "";

        public override void Init()
        {
            var stream = VirtualFile.Open(VirtualFile.PathToCpuInfo);

            byte flag = 0b0000000;
            const byte flagModelName = 0b0000001,
                       flagFreq      = 0b0000010,
                       flagCores     = 0b0000100;
            const byte flagAll = flagModelName | flagFreq | flagCores;

            do
            {
                string[] items = stream.ReadLine().Split(':');
                if (items.Length < 2)
                    continue;

                string value = items[1].Trim();

                switch (items[0])
                {
                    case "model name":
                        _value.Model = value;
                        flag |= flagModelName;
                        break;
                    case "cpu cores":
                        _value.Cores = int.Parse(value);
                        flag |= flagCores;
                        break;
                    case "cpu MHz":
                        _value.Frequence = double.Parse(value) / 1000.0;
                        flag |= flagFreq;
                        break;
                }
            } while ((flag ^ flagAll) > 0 && !stream.EndOfStream);

            stream.Close();

            _json = JsonConvert.SerializeObject(_value);
        }

        public override string GetJson()
        {
            return _json;
        }

        class CpuInfoData
        {
            public string Model { get; set; }
            public double Frequence { get; set; }
            public int Cores { get; set; }
        }
    }
}
