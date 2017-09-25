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

namespace MonitoringAgent
{
    class Agent
    {
        private const string settingsFilename = "settings.json";

        public static Settings Settings { get; private set; } = new Settings();

        private static List<IMonitor> _monitors = new List<IMonitor>();
        private static JsonHistory _history;
        private static TcpListener _listener;
        private static bool _runned;

        public static async void Start()
        {
            if (_runned)
                return;

            _runned = true;

            _Init();

            _listener = new TcpListener(IPAddress.Any, Settings.AgentPort);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();

            new Thread(async () =>
            {
                while (_runned)
                {
                    if (_listener.Pending())
                    {
                        try
                        {
                            var client = _listener.AcceptTcpClient();
                            ThreadPool.QueueUserWorkItem(_NewClientProcess);
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
            return "{" + _history.Take(Math.Min(Math.Max(last, 1), Settings.MaxReturn))
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

        private static void _Init()
        {
            _InitSettings();
            _InitMonitors();

            _history = new JsonHistory();
        }

        private static void _InitSettings()
        {
            if (!File.Exists(settingsFilename))
            {
                try
                {
                    File.WriteAllText(settingsFilename, JsonConvert.SerializeObject(Settings));
                }
                catch
                {
                    Console.WriteLine($"Error write to \"{Path.GetFullPath(settingsFilename)}\"!");
                    Environment.Exit(-1);
                }
                return;
            }

            try
            {
                Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFilename));
            }
            catch
            {
                Log.Critical($"Error read \"{Path.GetFullPath(settingsFilename)}\"!");
                Environment.Exit(-1);
            }

            if (!Directory.Exists(Settings.LogDir))
            {
                try
                {
                    Directory.CreateDirectory(Settings.LogDir);
                }
                catch
                {
                    Log.Critical($"Can't create directory \"{Path.GetFullPath(Settings.LogDir)}\"!");
                    Environment.Exit(-1);
                }
            }
        }

        private static void _InitMonitors()
        {
            var types = typeof(Agent).Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.GetInterfaces().Contains(typeof(IMonitor)) &&
                    type.CustomAttributes.Any(attr => attr.AttributeType == typeof(MonitorAttribute)))
                {
                    Log.Info($"Init Register monitor: {type.Name}");
                    var monitor = (IMonitor)Activator.CreateInstance(type);
                    monitor.Tag = ((MonitorAttribute)monitor.GetType().GetCustomAttributes(true).First(x => x is MonitorAttribute))
                                    .Tag;
                    _monitors.Add(monitor);
                }
            }

            _monitors.ForEach(monitor => monitor.Init());
        }
    }
}
