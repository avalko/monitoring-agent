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
        private const string HISTORY_PATH = "history.dat";

        public static Settings Settings { get; private set; } = new Settings();

        private List<IMonitor> _monitors = new List<IMonitor>();
        private TcpListener _listener;
        private bool _work;

        private List<HistoryItem> _history;
        private readonly DateTime _dateStartEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        private DateTime _lastHistorySave = DateTime.MinValue;

        struct HistoryItem
        {
            public static HistoryItem Null { get; } = new HistoryItem();

            public DateTime Time { get; set; }
            public string Json { get; set; }
        }

        public Agent()
        {
            const string settingsFilename = "settings.json";
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

            if (File.Exists(HISTORY_PATH))
            {
                try
                {
                    _history = File.ReadAllLines(HISTORY_PATH).Select(line =>
                    {
                        var arr = line.Split(';');
                        if (arr.Length < 2)
                            return HistoryItem.Null;
                        return new HistoryItem()
                        {
                            Time = _dateStartEpoch.AddSeconds(int.Parse(arr[0])),
                            Json = arr[1]
                        };
                    }).Where(x => !x.Equals(HistoryItem.Null)).ToList();
                }
                catch
                {
                    _history = new List<HistoryItem>();
                    Log.Warning($"Can't read history!");
                }
            }
            else
            {
                _history = new List<HistoryItem>();
            }

            _ClearHistoryIfOverflow();
        }

        public async void Start()
        {
            _work = true;
            _Init();

            _listener = new TcpListener(IPAddress.Any, Settings.AgentPort);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();

            new Thread(async () =>
            {
                while (_work)
                {
                    if (_listener.Pending())
                    {
                        try
                        {
                            var client = _listener.AcceptTcpClient();
                            ThreadPool.QueueUserWorkItem(async (_) =>
                            {
                                try
                                {
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
                                    if (data.StartsWith("GET /historyAll"))
                                    {
                                        returnData += GetHistoryJson();
                                    }
                                    else if (data.StartsWith("GET /history"))
                                    {
                                        int last = 100;
                                        if (data.Contains("?"))
                                        {
                                            var arr = data.SplitSpaces();
                                            if (arr.Length >= 3)
                                            {
                                                arr = arr[1].Split('?');
                                                if (arr.Length > 1)
                                                {
                                                    var get = HttpUtility.ParseQueryString(arr[1]);
                                                    if (get.AllKeys.Contains("last") && int.TryParse(get["last"], out int tmpLast))
                                                        last = tmpLast;
                                                }
                                            }
                                        }

                                        returnData += GetHistoryJson(last);
                                    }
                                    else
                                    {
                                        returnData += GetJson();
                                    }
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
                            });
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

            while (_work)
            {
                await Task.Delay(1000);
                try
                {
                    _monitors.ForEach(monitor => monitor.Update());
                    _history.Insert(0, new HistoryItem() { Time = DateTime.UtcNow, Json = GetJson() });
                    _ClearHistoryIfOverflow();
                }
                catch (Exception e)
                {
                    Log.Warning("Monitoring update error: " + e.ToString());
                }
            }
        }

        public string GetHistoryJson(int last = -1)
        {
            IEnumerable<HistoryItem> arr = null;

            if (last <= 0)
                arr = _history.Take(Math.Min(last, _history.Count));
            else
                arr = _history;

            return "{" + string.Join(',', arr.Select(item => $"\"{item.Time.Subtract(_dateStartEpoch).TotalSeconds}\": {item.Json}")) + "}";
        }

        public string GetJson()
        {
            return "{" + string.Join(',', _monitors.Select(monitor => $"\"{monitor.Tag}\": {monitor.GetJson()}")) + "}";
        }

        public void Stop()
        {
            _FlushHistory();

            _work = false;
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                }
                catch { }
            }
        }

        private void _FlushHistory()
        {
            try
            {
                File.WriteAllText(HISTORY_PATH, string.Join('\n', _history.Select(x => $"{DateTime.UtcNow.Subtract(_dateStartEpoch).TotalSeconds};{x.Json}")));
            }
            catch { }
        }

        private void _Init()
        {
            var types = this.GetType().Assembly.GetTypes();

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

        private void _ClearHistoryIfOverflow()
        {
            if (_history.Count > Settings.SaveHistory)
            {
                _history.RemoveRange(0, _history.Count - Settings.SaveHistory);
            }

        }
    }
}
