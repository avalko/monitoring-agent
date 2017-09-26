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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Log.Critical("Windows is not supported!");
                return 1;
            }

            string firstArgument = args.FirstOrDefault()?.TrimStart(new char[] { '-', '/' }) ?? "";

            switch (firstArgument)
            {
                case "init":
                case "i":
                    _InitMode();
                    break;
                case "daemon":
                case "d":
                    _NormalMode(true);
                    break;
                case "r":
                default:
                    _NormalMode(false);
                    break;
            }

            return 0;
        }

        private static void _InitMode()
        {
            Agent.Init(true);
            Console.WriteLine($"Your token: {Agent.Settings.TokenString}");
        }

        private static void _NormalMode(bool isDaemonMode)
        {
            AssemblyLoadContext.Default.Unloading += delegate
            {
                Agent.Stop();
                Log.Info("Good bye!");
            };

            Agent.Init();
            Agent.Start();

            if (Agent.Settings.DaemonMode || isDaemonMode)
            {
                Log.Info("Daemon started.");
                Thread.Sleep(Timeout.Infinite);
            }
            else
                Console.WriteLine("Press Enter to exit.");

            Console.Read();
            Agent.Stop();
        }
    }
}
