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
            var agent = new Agent();
            AssemblyLoadContext.Default.Unloading += delegate
            {
                agent.Stop();
                Log.Info("Good bye!");
            };

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Log.Critical("Only UNIX!");
                return 1;
            }

            if (Agent.Settings.DaemonMode || (args.Length > 0 && args[0] == "daemon"))
            {
                Log.Info("Daemon started.");
                Thread.Sleep(Timeout.Infinite);
            }
            else
                Console.WriteLine("Press Enter to exit.");

            Console.Read();
            agent.Stop();

            return 0;
        }
    }
}
