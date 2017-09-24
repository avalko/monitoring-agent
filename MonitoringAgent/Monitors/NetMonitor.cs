namespace MonitoringAgent.Monitors
{
    [Monitor("title")]
    class NetMonitor : BaseMonitor
    {
        public override void Init()
        {
            Json.Your = 0;
            Json.Custom = "hello";
            Json.Data = true;
        }

        public override void Update()
        {
            ++Json.Your;
        }
    }
}
