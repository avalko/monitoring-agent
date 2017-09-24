monitoring-agent
=====

## Monitor template:
```C#
namespace MonitoringAgent.Monitors
{
    [Monitor("test")]
    class TestMonitor : BaseMonitor
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
```
## Usage exmaple (using jQuery)
```JS
setInterval(function () {
  $.getJSON('localhost:5000', function (data) {
      console.log(data.test.Your);
      console.log(data.test.Custom);
      console.log(data.test.Data);
  });
}, 1000);
```
