using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitoringAgent.Monitors
{
    // diskstats
    [Monitor("disk")]
    class BlocksMonitor : BaseMonitor
    {
        private Dictionary<string, DiskastatsData> _disks = new Dictionary<string, DiskastatsData>();
        private Dictionary<string, BlockData> _blocks = new Dictionary<string, BlockData>();

        public override void Init()
        {
            /**
             *                               v                     v
             * major minor name rio rmerge rsect ruse wio wmerge wsect wuse running use aveq
             *   0     1     2   3     4     5     6   7     8     9     
             **/

            var disks = (VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats)).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = disk.SplitSpaces();

                string diskName = diskMatches[2] as string;
                BlockData block;

                Log.Debug($"Disk {diskName}");

                if (!char.IsNumber(diskName.Last()))
                {
                    var blockStat = VirtualFile.ReadLine(string.Format(VirtualFile.PathToBlockStat, diskName)).SplitSpaces();
                    int sectorSize = int.Parse(VirtualFile.ReadLine(string.Format(VirtualFile.PathToHWSectorSize, diskName)));
                    int blockSize = int.Parse(VirtualFile.ReadLine(string.Format(VirtualFile.PathToBlockSize, diskName)));

                    // Convert to megabytes
                    blockSize = (int)((blockSize / (1024.0 * 1024.0)) * sectorSize);

                    Log.Debug($" - Block ({sectorSize}bps) {blockSize}MiB");
                    _blocks[diskName] = block = new BlockData()
                    {
                        BlockSize = blockSize,
                        SectorSize = sectorSize,
                        LastTimeActive = int.Parse(blockStat[9])
                    };
                }
                else
                {
                    Log.Debug($" - Default");
                    block = _blocks.FirstOrDefault(x => diskName.StartsWith(x.Key)).Value;

                    if (block.BlockSize == 0)
                        continue;
                }

                int readBytes = int.Parse(diskMatches[5]) * block?.SectorSize ?? 1;
                int writBytes = int.Parse(diskMatches[9]) * block?.SectorSize ?? 1;

                _disks[diskName] = new DiskastatsData()
                {
                    LastReadBytes = readBytes,
                    LastWriteBytes = writBytes,
                    Block = block
                };
            }
        }

        public override void Update()
        {
            // Update blocks usage info (ms)
            foreach (var block in _blocks)
            {
                var blockStat = VirtualFile.ReadLine(string.Format(VirtualFile.PathToBlockStat, block.Key)).SplitSpaces();

                int timeActive = int.Parse(blockStat[9]);
                int timeWait = int.Parse(blockStat[10]);

                block.Value.Active = (timeActive - block.Value.LastTimeActive) * 0.1;

                block.Value.LastTimeActive = timeActive;
            }

            // Update all disks info (read/write bytes per sec)
            var disks = VirtualFile.ReadToEnd(VirtualFile.PathToDiskStats).SplitLines();
            foreach (var disk in disks)
            {
                var diskMatches = disk.SplitSpaces();
                string diskName = diskMatches[2] as string;

                var data = _disks[diskName];

                int currentBytesReads = int.Parse(diskMatches[5]) * data.Block.SectorSize;
                int currentBytesWrites = int.Parse(diskMatches[9]) * data.Block.SectorSize;

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
            return JsonConvert.SerializeObject(_disks);
        }

        class BlockData
        {
            public int SectorSize { get; set; }
            public int BlockSize { get; set; }

            public double Active { get; set; }
            public int LastTimeActive { get; set; }
        }

        class DiskastatsData
        {
            public int ReadBytes { get; set; }
            public int WriteBytes { get; set; }
            public int MaxReadBytes { get; set; }
            public int MaxWriteBytes { get; set; }
            public int Size => Block?.BlockSize ?? 0;
            public double Active => Block?.Active ?? 0;
            
            [JsonIgnore]
            public BlockData Block { get; set; }
            [JsonIgnore]
            public int LastReadBytes { get; set; }
            [JsonIgnore]
            public int LastWriteBytes { get; set; }
        }
    }
}
