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

                if (args.Length > 1)
                {
                    if (File.Exists(args[1]))
                    {
                        try
                        {
                            File.WriteAllText(args[1], "");
                            fileOut = args[1];
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unable to open file \"{args[1]}\" for writing!");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }
                    else
                    {
                        try
                        {
                            File.WriteAllText(args[1], "");
                            fileOut = args[1];
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            if (args[1].Intersect(Path.GetInvalidFileNameChars()).Count() > 0)
                                Console.WriteLine($"Invalid filename!");
                            else
                                Console.WriteLine($"Unable create file \"{args[1]}\"!");
                            Console.ForegroundColor = ConsoleColor.Gray;

                            return 1;
                        }
                    }
                }
            }

            new Agent().Start(timeout, fileOut);
            Thread.Sleep(Timeout.Infinite);

            return 0;
        }
    }
}
