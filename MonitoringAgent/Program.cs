using System;
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

#if RELEASE
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine("Only UNIX!");
                return 1;
            }
#else
            // Nothing...
#endif

            int timeout = 1000;

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int newTimeout))
                    timeout = newTimeout;
            }

            new Agent().Start(timeout);
            Thread.Sleep(Timeout.Infinite);

            return 0;
        }
    }
}
