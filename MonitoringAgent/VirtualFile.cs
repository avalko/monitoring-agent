using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MonitoringAgent
{
    public static class VirtualFile
    {
        public async static Task<string> ReadLineAsync(string filePath)
        {
            using (var stream = File.OpenText(filePath))
            {
                return await stream.ReadLineAsync();
            }
        }

        public async static Task<string> ReadToEndAsync(string filePath)
        {
            using (var stream = File.OpenText(filePath))
            {
                return await stream.ReadToEndAsync();
            }
        }
    }
}
