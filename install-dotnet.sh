apt-get update
apt-get install --assume-yes curl libunwind8 gettext apt-transport-https
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg

if [ -f /etc/os-release ]; then
    . /etc/os-release
    VER=$VERSION_ID
else
    VER=8
fi

if [ $VER -gt 8 ]; then
    echo 'Debian 9 (Stretch)'
    sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/dotnetdev.list'
else
    echo 'Debian 8 (Jessie)'
    sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-jessie-prod jessie main" > /etc/apt/sources.list.d/dotnetdev.list'
fi

apt-get update
apt-get install --assume-yes dotnet-sdk-2.0.0

