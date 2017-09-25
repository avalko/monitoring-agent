using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;

namespace MonitoringAgent
{
    class Program
    {
        static int Main(string[] args)
        {
            AssemblyLoadContext.Default.Unloading += delegate
            {
                Agent.Stop();
                Log.Info("Good bye!");
            };

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Log.Critical("Only UNIX!");
                return 1;
            }

            Agent.Start();

            if (Agent.Settings.DaemonMode || args.FirstOrDefault() == "daemon")
            {
                Log.Info("Daemon started.");
                Thread.Sleep(Timeout.Infinite);
            }
            else
                Console.WriteLine("Press Enter to exit.");

            Console.Read();
            Agent.Stop();

            return 0;
        }
    }
}
