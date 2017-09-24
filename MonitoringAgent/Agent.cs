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

namespace MonitoringAgent
{
    class Agent
    {
        public static Settings Settings { get; private set; } = new Settings();

        private List<IMonitor> _monitors = new List<IMonitor>();
        private TcpListener _listener;
        private bool _work;
        private StringBuilder _sb = new StringBuilder();

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
                Console.WriteLine($"Error read \"{Path.GetFullPath(settingsFilename)}\"!");
                Environment.Exit(-1);
            }
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
                                    string returnData = "HTTP/1.1 200 OK\n\n" + GetJson();
                                    byte[] returnBytes = Encoding.UTF8.GetBytes(returnData);
                                    var stream = client.GetStream();
                                    await stream.WriteAsync(returnBytes, 0, returnBytes.Length);
                                    stream.Close();
                                    client.Close();
                                }
                                catch { }
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
                }
                catch (Exception e)
                {
                    Log.Warning("Monitoring update error: " + e.ToString());
                }
            }
        }

        public string GetJson()
        {
            _sb.Clear();
            _sb.Append("{");
            _sb.Append(string.Join(',', _monitors.Select(monitor => $"\"{monitor.Tag}\": {monitor.GetJson()}")));
            _sb.Append("}\n");

            return _sb.ToString();
        }

        public void Stop()
        {
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

        private void _Init()
        {
            var types = this.GetType().Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.IsInstanceOfType(typeof(IMonitor)) &&
                    type.CustomAttributes.Any(attr => attr.AttributeType == typeof(MonitorAttribute)))
                {
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
