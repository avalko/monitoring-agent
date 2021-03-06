﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MonitoringAgent
{
    class MonitorAttribute : Attribute
    {
        private string _title;
        public string Tag => _title;

        public MonitorAttribute(string title)
        {
            _title = title;
        }
    }
}
