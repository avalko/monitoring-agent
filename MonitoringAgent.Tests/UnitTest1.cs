using Microsoft.VisualStudio.TestTools.UnitTesting;

using MonitoringAgent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Threading;

namespace MonitoringAgent.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestInit()
        {
            if (File.Exists(Agent.SettingsFilename))
            {
                File.Delete(Agent.SettingsFilename);
            }
            Agent.Init();
            Assert.IsTrue(File.Exists(Agent.SettingsFilename));
        }

        [TestMethod]
        
        public void TestWork()
        {
            Agent.Init();
            Agent.Start();
            Thread.Sleep(1000);
            string data = HttpGet($"http://localhost:{Agent.Settings.AgentPort}/");
            dynamic json = JObject.Parse(data);
            Assert.IsTrue(json.cpu.Cores == System.Environment.ProcessorCount);
            Assert.IsTrue(json.mem.Total > 0);
        }

        private static string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();
            var responseReader = new StreamReader(webStream);
            var response = responseReader.ReadToEnd();
            responseReader.Close();
            return response;
        }
    }
}
