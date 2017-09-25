using Microsoft.VisualStudio.TestTools.UnitTesting;

using MonitoringAgent;
using System.IO;
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
            //Agent.Start();
            Thread.Sleep(100);
            Assert.IsTrue(false);
        }
    }
}
