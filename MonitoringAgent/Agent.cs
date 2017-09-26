using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Reflection;

namespace MonitoringAgent
{
    public static class Agent
    {
        public const string SettingsFilename = "settings.json";
        public const string SettingsFilenameBackup = "settings-backup-{0}.json";

        public static Settings Settings { get; private set; }

        private static List<IMonitor> _monitors = new List<IMonitor>();
        private static JsonHistory _history;
        private static TcpListener _listener;
        private static bool _runned;

        /// <summary>
        /// Initializing the monitoring agent (settings, logging, monitors)
        /// </summary>
        public static void Init(bool onlySettings = false)
        {
            _Init(onlySettings);
        }

        /// <summary>
        /// 1. The HTTP server is started.
        /// 2. All monitors update their state every second.
        /// </summary>
        public static async void Start()
        {
            // If the server is already running, we exit.
            if (_runned)
                return;

            // At this point, the settings must be loaded/initialized.
            if (Settings == null)
                throw new NullReferenceException(nameof(Settings));

            _runned = true;

            _listener = new TcpListener(IPAddress.Any, Settings.AgentPort);
            // IMPORTANT. Otherwise, with an abnormal termination, we can get exception "Adress already in use"
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();

            // Starting an infinite loop of the HTTP server.
            new Thread(async () =>
            {
                // Continue to execute only while the server is running.
                while (_runned)
                {
                    // IMPORTANT. Otherwise AcceptTcpClient blocks the thread and
                    // we can not shut down the server correctly.
                    if (_listener.Pending())
                    {
                        try
                        {
                            var client = _listener.AcceptTcpClient();
                            // Sarting the new client's handler in the new thread from the pool.
                            // Theoretically, many threads will not be executed simultaneously.
                            // Because the load on the monitoring server should not be high.
                            // So, not need to wait long for the thread to free :)
                            ThreadPool.QueueUserWorkItem(_NewClientProcess, client);
                            // To not wait 100ms before checking for a new connection.
                            continue;
                        }
                        catch (Exception e)
                        {
                            Log.Warning("TCP Server Error: " + e.ToString());
                        }
                    }

                    await Task.Delay(100);
                }
            }).Start();

            Log.Info("TCP server started.");

            // Starting the main server.
            // Every second we update the status of monitors.
            while (_runned)
            {
                await Task.Delay(1000);
                try
                {
                    _monitors.ForEach(monitor => monitor.Update());
                    _history.Insert(_GetJsonWithoutStatic());
                    _history.ClearHistoryIfOverflow();
                }
                catch (Exception e)
                {
                    Log.Warning("Monitoring update error: " + e.ToString());
                }

                _history.AutoSave();
            }
        }

        public static void Stop()
        {
            if (!_runned)
                return;

            _runned = false;
            _history.Dispose();
            _history = null;

            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener = null;
                }
                catch { }
            }
        }

        private static string _GetJson()
        {
            return "{" + string.Join(',', _monitors.Select(monitor => $"\"{monitor.Tag}\": {monitor.GetJson()}")) + "}";
        }

        private static string _GetJsonWithoutStatic()
        {
            return "{" + string.Join(',', _monitors.Where(x => !x.Static).Select(monitor => $"\"{monitor.Tag}\": {monitor.GetJson()}")) + "}";
        }

        private static string _GetHistoryJson(int last)
        {
            return "{" + _history.Take(Math.Min(Math.Max(last, 1), Settings.MaxReturnHistoryItems))
                                 .Select(item => $"\"{item.TimeStamp}\": {item.Json}").JoinString() + "}";
        }

        private static async void _NewClientProcess(object arg)
        {
            try
            {
                TcpClient client = arg as TcpClient;
                byte[] buffer = new byte[512];
                var stream = client.GetStream();
                string data = "";
                int length = 0;
                if ((length = stream.Read(buffer, 0, 512)) > 0)
                {
                    data += Encoding.ASCII.GetString(buffer, 0, length);
                }

                string returnData = "HTTP/1.1 200 OK\n\n";
                byte[] returnBytes;

                string firstLine = data.SplitLines().First();
                string[] fullRequest = firstLine.SplitSpaces().Skip(1).First().Split('?');
                string request = fullRequest.First();

                if (request == "history")
                {
                    string query = fullRequest.Skip(1).JoinString();
                    int last = 100;
                    if (!string.IsNullOrEmpty(query))
                    {
                        var get = HttpUtility.ParseQueryString(query);
                        if (get.HasKeys() && get.AllKeys.Contains("last") &&
                            int.TryParse(get["last"], out int tmpLast))
                        {
                            last = tmpLast;
                        }
                    }

                    returnData += _GetHistoryJson(last);
                }
                else
                    returnData += _GetJson();


                returnBytes = Encoding.UTF8.GetBytes(returnData);
                await stream.WriteAsync(returnBytes, 0, returnBytes.Length);

                stream.Close();
                client.Close();
                buffer = null;
            }
            catch (Exception e)
            {
                Log.Warning("TCP Thread Error: " + e.ToString());
            }
        }

        private static void _Init(bool onlySettings)
        {
            if (onlySettings)
            {
                _InitSettings(true);
                return;
            }
            else
            {
                _InitLogging();
                _InitSettings(false);
            }
            _InitMonitors();

            _history = new JsonHistory();
        }

        private static void _InitSettings(bool forceRewrite)
        {
            if (forceRewrite)
            {
                if (File.Exists(SettingsFilename))
                {
                    Console.WriteLine("Settings already exist. Overwrite? (enter \"yes\" or \"y\")");
                    string input = Console.ReadLine();
                    if (input == "y" || input == "yes")
                    {
                        _WriteDefaultToSettingsFile();
                    }
                    else
                    {
                        Console.WriteLine("Ok. Good bye.");
                        Environment.Exit(0);
                    }
                }
                return;
            }

            if (!File.Exists(SettingsFilename))
            {
                _WriteDefaultToSettingsFile();
                return;
            }

            string settingsInJson = "";
            try
            {
                settingsInJson = File.ReadAllText(SettingsFilename);
            }
            catch
            {
                Log.Critical($"Error reading file: \"{Path.GetFullPath(SettingsFilename)}\"!");
                Environment.Exit(-1);
            }

            try
            {
                Settings = JsonConvert.DeserializeObject<Settings>(settingsInJson);

                if (!Settings.IsCorrect())
                {
                    Log.Critical("The settings file is not in the correct format! Settings have been reset!");

                    try
                    {
                        File.Copy(SettingsFilename, string.Format(SettingsFilenameBackup, DateTime.Now.ToString("yyyyMMddHHmmss")));
                    }
                    catch
                    {
                        Log.Critical($"Error saving old settings!");
                        Environment.Exit(-1);
                    }

                    _WriteDefaultToSettingsFile();
                }
            }
            catch
            {
                Log.Critical($"Loading error: \"{Path.GetFullPath(SettingsFilename)}\"!");
                Environment.Exit(-1);
            }
        }

        private static void _WriteDefaultToSettingsFile()
        {
            try
            {
                Settings = new Settings();
                File.WriteAllText(SettingsFilename, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            catch
            {
                Console.WriteLine($"Error writing to file \"{Path.GetFullPath(SettingsFilename)}\"!");
                Environment.Exit(-1);
            }
        }

        private static void _InitLogging()
        {
            Log.Init();
        }

        private static void _InitMonitors()
        {
            var types = typeof(Agent).Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.GetInterfaces().Contains(typeof(IMonitor)) &&
                    type.CustomAttributes.Any(attr => attr.AttributeType == typeof(MonitorAttribute)))
                {
                    Log.Info($"Registered monitor: {type.Name}");
                    var monitor = (IMonitor)Activator.CreateInstance(type);
                    monitor.Tag = ((MonitorAttribute)monitor.GetType().GetTypeInfo().GetCustomAttribute<MonitorAttribute>())
                                    .Tag;
                    _monitors.Add(monitor);
                }
            }

            _monitors.ForEach(monitor => monitor.Init());
        }
    }
}
