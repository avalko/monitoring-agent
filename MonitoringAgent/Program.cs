using System;
using System.IO;
using System.Runtime.Loader;
using System.Threading;

namespace MonitoringAgent
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Started monitoring...");
            AssemblyLoadContext.Default.Unloading += delegate
            {
                Console.WriteLine("Good bye!");
            };
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine("Only UNIX!");
                return 1;
            }

            Console.WriteLine(File.ReadAllText("/sys/block/sda/queue/hw_sector_size"));
            Console.WriteLine(File.ReadAllText("‌/proc/uptime"));
            Console.WriteLine(File.ReadAllText("/sys/class/net/eth0/statistics/tx_packets"));

            //while (true)
            {
                // Every second

                Thread.Sleep(1000);
            }

            return 0;
        }
    }
}
