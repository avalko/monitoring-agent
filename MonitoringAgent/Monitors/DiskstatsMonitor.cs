using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor("disk")]
    class DiskstatsMonitor : BaseMonitor
    {
        private Scanf _scanf;
        private int _hwSectorSize;
        private Dictionary<string, DiskastatsData> _values = new Dictionary<string, DiskastatsData>();

        public override void Init()
        {
            /**
             *                               v                     v
             * major minor name rio rmerge rsect ruse wio wmerge wsect wuse running use aveq
             *   0     1     2   3     4     5     6   7     8     9     
             **/
            _scanf = Scanf.Create("%i %i %s %i %i %i %i %i %i %i %i %i %i %i");
            _hwSectorSize = int.Parse(VirtualFile.ReadLine(VirtualFile.PathToHWSectorSize));

            var disks = (VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats)).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                int readBytes = (int)diskMatches[5] * _hwSectorSize;
                int writBytes = (int)diskMatches[9] * _hwSectorSize;

                _values[diskMatches[2] as string] = new DiskastatsData()
                {
                    LastReadBytes = readBytes,
                    LastWriteBytes = writBytes,
                };
            }
        }

        public override void Update()
        {
            var disks = VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                var data = _values[diskMatches[2] as string];

                int currentBytesReads = (int)diskMatches[5] * _hwSectorSize;
                int currentBytesWrites = (int)diskMatches[9] * _hwSectorSize;

                data.ReadBytes = currentBytesReads - data.LastReadBytes;
                data.WriteBytes = currentBytesWrites - data.LastWriteBytes;

                if (data.ReadBytes > data.MaxReadBytes)
                    data.MaxReadBytes = data.ReadBytes;
                if (data.WriteBytes > data.MaxWriteBytes)
                    data.MaxWriteBytes = data.WriteBytes;

                data.LastReadBytes = currentBytesReads;
                data.LastWriteBytes = currentBytesWrites;
            }
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_values);
        }

        class DiskastatsData
        {
            public int ReadBytes { get; set; }
            public int WriteBytes { get; set; }
            public int MaxReadBytes { get; set; }
            public int MaxWriteBytes { get; set; }

            [JsonIgnore]
            public int LastReadBytes { get; set; }
            [JsonIgnore]
            public int LastWriteBytes { get; set; }
        }
    }
}
