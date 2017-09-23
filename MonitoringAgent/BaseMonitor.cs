﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    class BaseMonitor : IMonitor
    {
        protected Scanf _scanf { get; set; }

        public virtual string GetJson()
        {
            return "[]";
        }

        public virtual void Init()
        {
        }

        public virtual void Next()
        {
        }

        public virtual void Update()
        {
        }
    }
}
