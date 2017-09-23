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
        private int _hwSectorSize;
        private Dictionary<string, DiskastatsData> _values = new Dictionary<string, DiskastatsData>();

        public override void Init()
        {
            /**
             * major minor name RIO rmerge rsect ruse WIO wmerge wsect wuse running use aveq
             **/
            _scanf = Scanf.Create("%i %i %s %i %i %i %i %i %i %i %i %i %i %i");
            _hwSectorSize = int.Parse(VirtualFile.ReadLine(VirtualFile.PathToHWSectorSize));

            var disks = (VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats)).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                int readBytes = (int)diskMatches[3] * _hwSectorSize;
                int writBytes = (int)diskMatches[7] * _hwSectorSize;

                _values[diskMatches[2] as string] = new DiskastatsData()
                {
                    LastReadBytes = readBytes,
                    LastRealReadBytes = readBytes,
                    LastWriteBytes = writBytes,
                    LastRealWriteBytes = writBytes,
                };
            }
        }

        public override void Next()
        {
            var disks = VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                var data = _values[diskMatches[2] as string];

                int currentBytesReads = (int)diskMatches[3] * _hwSectorSize;
                int currentBytesWrites = (int)diskMatches[7] * _hwSectorSize;

                data.ReadBytes = currentBytesReads - data.LastReadBytes;
                data.WriteBytes = currentBytesWrites - data.LastWriteBytes;

                if (data.ReadBytes > data.MaxReadBytes)
                    data.MaxReadBytes = data.ReadBytes;
                if (data.WriteBytes > data.MaxWriteBytes)
                    data.MaxWriteBytes = data.WriteBytes;

                data.LastRealReadBytes = currentBytesReads;
                data.LastRealWriteBytes = currentBytesWrites;
            }
        }

        public override void Update()
        {
            foreach (var disk in _values)
            {
                disk.Value.LastReadBytes = disk.Value.LastRealReadBytes;
                disk.Value.LastWriteBytes = disk.Value.LastRealWriteBytes;
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
            [JsonIgnore]
            public int LastRealReadBytes { get; set; }
            [JsonIgnore]
            public int LastRealWriteBytes { get; set; }
        }
    }
}
