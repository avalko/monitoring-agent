using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    class Agent
    {
        private static List<IMonitor> _monitors = new List<IMonitor>();

        public void Start(int timeout, string fileOut)
        {
            _Init();

            while (true)
            {
                Thread.Sleep(timeout);

                _monitors.ForEach(monitor => monitor.Update());

                if (fileOut != null)
                {
                    _monitors.ForEach(monitor => File.WriteAllText(fileOut,
                        (monitor.GetType().GetCustomAttributes(true).First(x => x is MonitorAttribute) as MonitorAttribute)
                        .Title + ": " + monitor.GetJson()));
                }
                else
                {
                    Console.Clear();
                    _monitors.ForEach(monitor => Console.WriteLine(
                        (monitor.GetType().GetCustomAttributes(true).First(x => x is MonitorAttribute) as MonitorAttribute)
                        .Title + ": " + monitor.GetJson()));
                }

                _monitors.ForEach(monitor => monitor.Update());
            }
        }

        private void _Init()
        {
            var types = this.GetType().Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.CustomAttributes.Any(attr => attr.AttributeType == typeof(MonitorAttribute)))
                {
                    _monitors.Add((IMonitor)Activator.CreateInstance(type));
                }
            }

            _monitors.ForEach(monitor => monitor.Init());
        }
    }
}
