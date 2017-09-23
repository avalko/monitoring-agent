using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    class Agent
    {
        private static List<IMonitor> _monitors = new List<IMonitor>();

        public void Start()
        {
            _Init();

            while (true)
            {
                Thread.Sleep(1000);
                // Every second
                _monitors.ForEach(monitor => monitor.Next());

                Console.Clear();
                _monitors.ForEach(monitor => Console.WriteLine(monitor.GetJson()));
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
