using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MonitoringAgent
{
    public static class Log
    {
        public static void Info(string message)
        {
            WriteLine("Information", message);
        }

        public static void Warning(string message)
        {
            WriteLine("- Warning -", message);
        }

        public static void Critical(string message)
        {
            WriteLine("---CRITICAL---", message);
        }

        public static void WriteLine(string title, string message)
        {
            string data = $"[{DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}]" +
                          $"{title.PadLeft(50)}\n{new string(' ', 25)}{message}";
            Append(data);
        }

        private static void Append(string raw)
        {
            Console.WriteLine(raw);
            if (Agent.Settings.LogEnable)
                File.AppendAllText(DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + Agent.Settings.LogFile,
                                   raw + "\n" + new string('-', 25) + "\n");
        }
    }
}
