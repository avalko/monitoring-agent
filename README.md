monitoring-agent v1.0
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

# Install
### [.NET Core 2.0](https://www.microsoft.com/net/core)
Install on debian:
```BASH
apt-get update
apt-get install --assume-yes curl libunwind8 gettext apt-transport-https
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
# Debian 9 (Stretch)
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/dotnetdev.list'
# Debian 8 (Jessie)
# sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/dotnetdev.list'
apt-get update
apt-get install dotnet-sdk-2.0.0
```

### Agent
```BASH
git clone git@github.com:avalko/monitoring-agent.git
# or
# git clone https://github.com/avalko/monitoring-agent.git
cd monitoring-agent/MonitoringAgent
dotnet restore
dotnet publish -c Release
# Run
dotnet bin/Release/netcoreapp2.0/MonitoringAgent.dll
```
