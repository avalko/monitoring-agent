using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    [Monitor]
    class DiskstatsMonitor : BaseMonitor
    {
#if RELEASE
        public override string PathToFile = "/proc/diskstats";
        private const string _pathToHwSectorSize = "/sys/block/sda/queue/hw_sector_size";
#else
        public override string PathToFile => "Examples/diskstats.txt";
        private const string _pathToHwSectorSize = "Examples/hw_sector_size.txt";
#endif

        private int _hwSectorSize;
        private Scanf _scanf;

        class DiskastatsData
        {
            public int ReadBytes { get; set; }
            public int WriteBytes { get; set; }

            [JsonIgnore]
            public int LastReadBytes { get; set; }
            [JsonIgnore]
            public int LastWriteBytes { get; set; }
        }

        private Dictionary<string, DiskastatsData> _values = new Dictionary<string, DiskastatsData>();

        public override async void Init()
        {
            // major minor name RIO rmerge rsect ruse WIO wmerge wsect wuse running use aveq
            _scanf = Scanf.Create("%i %i %s %i %i %i %i %i %i %i %i %i %i %i");
            _hwSectorSize = int.Parse(await VirtualFile.ReadLineAsync(_pathToHwSectorSize));

            var disks = (await _ReadToEndAsync()).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                _values[diskMatches[2] as string] = new DiskastatsData()
                {
                    LastReadBytes = (int)diskMatches[3] * _hwSectorSize,
                    LastWriteBytes = (int)diskMatches[7] * _hwSectorSize,
                };
            }
        }

        public override async void Next()
        {
            var disks = (await _ReadToEndAsync()).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = _scanf.Matches(disk);
                var data = _values[diskMatches[2] as string];

                int currentBytesReads = (int)diskMatches[3] * _hwSectorSize;
                int currentBytesWrites = (int)diskMatches[7] * _hwSectorSize;

                data.ReadBytes = currentBytesReads - data.LastReadBytes;
                data.WriteBytes = currentBytesWrites - data.LastWriteBytes;

                data.LastReadBytes = currentBytesReads;
                data.LastWriteBytes = currentBytesWrites;
            }
        }

        public override string GetJson()
        {
            return JsonConvert.SerializeObject(_values);
        }
    }
}
