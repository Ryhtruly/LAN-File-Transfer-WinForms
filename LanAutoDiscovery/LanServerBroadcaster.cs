using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanAutoDiscovery;

public sealed class LanServerBroadcaster : IAsyncDisposable
{
    private readonly string _serverId = Guid.NewGuid().ToString("N");
    private readonly string _displayName;
    private readonly int _serverPort;
    private readonly int _discoveryPort;
    private readonly TimeSpan _broadcastInterval;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _broadcastTask;

    public LanServerBroadcaster(
        string displayName,
        int serverPort,
        int discoveryPort = 40404,
        TimeSpan? broadcastInterval = null)
    {
        _displayName = displayName;
        _serverPort = serverPort;
        _discoveryPort = discoveryPort;
        _broadcastInterval = broadcastInterval ?? TimeSpan.FromSeconds(3);
    }

    public void Start()
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient
        {
            EnableBroadcast = true
        };

        _broadcastTask = Task.Run(() => BroadcastLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        if (_broadcastTask is not null)
        {
            try
            {
                await _broadcastTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _udpClient?.Dispose();
        _cts.Dispose();

        _udpClient = null;
        _cts = null;
        _broadcastTask = null;
    }

    private async Task BroadcastLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var ipAddress = GetBestLocalIpv4Address();
            if (ipAddress is not null)
            {
                var packet = new LanDiscoveryPacket
                {
                    ServerId = _serverId,
                    DisplayName = _displayName,
                    IpAddress = ipAddress,
                    Port = _serverPort
                };

                var payload = Encoding.UTF8.GetBytes(LanDiscoveryPacket.Serialize(packet));
                await _udpClient!.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, _discoveryPort))
                    .ConfigureAwait(false);
            }

            await Task.Delay(_broadcastInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? GetBestLocalIpv4Address()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        foreach (var networkInterface in interfaces)
        {
            var address = networkInterface.GetIPProperties().UnicastAddresses
                .FirstOrDefault(ip =>
                    ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip.Address));

            if (address is not null)
            {
                return address.Address.ToString();
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
