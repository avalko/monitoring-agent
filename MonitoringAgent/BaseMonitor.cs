using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    class BaseMonitor : IMonitor
    {
        protected dynamic Json = new ExpandoObject();

        public string Tag { get; set; } = "";

        public virtual string GetJson()
        {
            return JsonConvert.SerializeObject(Json);
        }

        public virtual void Init()
        {
        }

        public virtual void Update()
        {
        }
    }
}
