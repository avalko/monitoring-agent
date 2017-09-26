﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MonitoringAgent
{
    public static class Log
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (!Directory.Exists(Agent.Settings.LoggingDirectory))
            {
                try
                {
                    Directory.CreateDirectory(Agent.Settings.LoggingDirectory);
                    _initialized = true;
                }
                catch
                {
                    Log.Critical($"Can't create directory \"{Path.GetFullPath(Agent.Settings.LoggingDirectory)}\"!");
                    Environment.Exit(-1);
                }
            }
        }

        public static void Info(string message)
        {
            WriteLine("Information", message);
        }

        public static void Debug(string message)
        {
#if DEBUG
            WriteLine("Debug", message);
#endif
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
                            new string(' ', 15) +
                            $"{title}\n{new string(' ', 25)}{message}";
            Append(data);
        }

        private static void Append(string raw)
        {
            Console.WriteLine(raw);
            if (_initialized && Agent.Settings.LoggingEnabled)
                File.AppendAllText(Agent.Settings.LoggingDirectory + "/" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log",
                                   raw + "\n" + new string('-', 50) + "\n");
        }
    }
}
