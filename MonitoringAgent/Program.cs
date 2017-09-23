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
                Console.WriteLine("Good bye!");
            };

#if RELEASE
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine("Only UNIX!");
                return 1;
            }
#else
            // Nothing...
#endif
            
            int port = 5000;

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int newPort) && newPort > 0)
                {
                    port = newPort;                    
                }
            }

            Console.WriteLine("Started monitoring...");
            agent.Start(port);

            if (args.Length > 1 && args[1] == "daemon")
            {
                Console.WriteLine("Daemon started.");
                Thread.Sleep(Timeout.Infinite);
            }

            Console.WriteLine("Press Enter to exit.");
            Console.Read();
            agent.Stop();

            return 0;
        }
    }
}
