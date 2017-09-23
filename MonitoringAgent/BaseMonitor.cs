using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    class BaseMonitor : IMonitor
    {
        public virtual string PathToFile => throw new NotImplementedException();

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

        protected async Task<string> _ReadToEndAsync()
        {
            return await VirtualFile.ReadToEndAsync(PathToFile);
        }

        protected async Task<string> _ReadLineAsync()
        {
            return await VirtualFile.ReadLineAsync(PathToFile);
        }
    }
}
