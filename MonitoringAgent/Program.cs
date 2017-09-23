using System;
using System.Runtime.Loader;
using System.Threading;

namespace MonitoringAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started monitoring...");
            AssemblyLoadContext.Default.Unloading += delegate
            {
                Console.WriteLine("Good bye!");
            };
            
            while (true)
            {
                // Every second

                Thread.Sleep(1000);
            }
        }
    }
}
