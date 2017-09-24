using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MonitoringAgent
{
    public static class VirtualFile
    {
        public const string PathToNetDev        = "/proc/net/dev";
        public const string PathToNetSnmp       = "/proc/net/snmp";
        public const string PathToHWSectorSize  = "/sys/block/{0}/queue/hw_sector_size";
        public const string PathToBlockStat     = "/sys/block/{0}/stat";
        public const string PathToBlockSize     = "/sys/block/{0}/size";
        public const string PathToDiskStats     = "/proc/diskstats";
        public const string PathToProcStat      = "/proc/stat";
        public const string PathToMemInfo       = "/proc/meminfo";
        public const string PathToUpTime        = "/proc/uptime";
        public const string PathToCpuInfo       = "/proc/cpuinfo";


        public static StreamReader Open(string filePath)
        {
            try
            {
                return File.OpenText(filePath);
            }
            catch (Exception e)
            {
                Log.Critical($"Error Open file \"{filePath}\": {e}");
                throw;
            }
        }

        public static string ReadLine(string filePath)
        {
            try
            {
                using (var stream = File.OpenText(filePath))
                {
                    return stream.ReadLine();
                }
            }
            catch (Exception e)
            {
                Log.Critical($"Error ReadLine from file \"{filePath}\": {e}");
                throw;
            }
        }

        public static string ReadToEnd(string filePath)
        {
            try
            {
                using (var stream = File.OpenText(filePath))
                {
                    return stream.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Log.Critical($"Error ReadToEnd from file \"{filePath}\": {e}");
                throw;
            }
        }
    }
}
