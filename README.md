[![Build Status](https://travis-ci.org/avalko/monitoring-agent.svg?branch=master)](https://travis-ci.org/avalko/monitoring-agent)

monitoring-agent v1.0
=====
Monitoring your server.

# Requirements
### [.NET Core 2.0](https://www.microsoft.com/net/core)
Install .NET Core on debian:
```BASH
apt-get update
apt-get install curl libunwind8 gettext apt-transport-https
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
# Debian 9 (Stretch)
sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/dotnetdev.list'
# Debian 8 (Jessie)
# sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/dotnetdev.list'
apt-get update
apt-get install dotnet-sdk-2.0.0
```

## Install agent
```BASH
git clone https://github.com/avalko/monitoring-agent.git
cd monitoring-agent
# First install or big updates
./full-build.sh
# Create settings.json (first init)
./init.sh
# [Output example] Your token: 917700A0CB394840992BF2142A92DDE7
# Minore update
./build.sh
```
## Run
```BASH
# You must be in the project directory
cd monitoring-agent
./run.sh
```

## Info
Two files are created in the working directory:
#### settings.json
At the moment the following variables are available:
+ AgentPort (default 5000)
+ LoggingEnabled (default true)
+ LoggingDirectory (deafult "logs")
+ DaemonMode (default false)<br>
    *If true is specified, the program will wait for the end signal (Ctrl+C or `service magent stop`)*
+ SaveHistorySeconds (default 2678400)<br>
    *Specifies for which period to store the history in the "history.dat" file in seconds.*<br>
    ***Format:***<br>
    `unix timestamp;{"prop": {...}, ...}`
+ AutoSaveHistorySeconds
+ AutoSaveHistoryEnabled
+ MaxReturnHistoryItems
+ TokenString

You can always create a corrected file with the settings using ./init.sh<br>
All available variables will appear there.
#### history.dat


## Monitor template:
You can create your own monitor.
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
var token = '917700A0CB394840992BF2142A92DDE7';
setInterval(function () {
  $.getJSON('localhost:5000/' + token, function (data) {
      console.log(data.test.Your);
      console.log(data.test.Custom);
      console.log(data.test.Data);
  });
}, 1000);
```
