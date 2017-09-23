using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MonitoringAgent
{
    public static class VirtualFile
    {
        public const string PathToHWSectorSize  = "/sys/block/sda/queue/hw_sector_size";
        public const string PathToDiskStats     = "/proc/diskstats";
        public const string PathToProcStat      = "/proc/stat";


        public static string ReadLine(string filePath)
        {
            using (var stream = File.OpenText(filePath))
            {
                return stream.ReadLine();
            }
        }

        public static string ReadToEnd(string filePath)
        {
            using (var stream = File.OpenText(filePath))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
