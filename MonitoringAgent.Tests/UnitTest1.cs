using Microsoft.VisualStudio.TestTools.UnitTesting;

using MonitoringAgent;
using System.IO;

namespace MonitoringAgent.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            if (File.Exists(Agent.SettingsFilename))
            {
                File.Delete(Agent.SettingsFilename);
            }

            Agent.Init();

            Assert.IsTrue(File.Exists(Agent.SettingsFilename));
        }
    }
}
