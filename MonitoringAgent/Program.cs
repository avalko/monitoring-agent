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
            string fileOut = null;

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int newTimeout) && newTimeout > 0)
                {
                    timeout = newTimeout;                    
                }

                if (args.Length > 1 && File.Exists(args[1]))
                    fileOut = args[1];
            }

            new Agent().Start(timeout, fileOut);
            Thread.Sleep(Timeout.Infinite);

            return 0;
        }
    }
}
