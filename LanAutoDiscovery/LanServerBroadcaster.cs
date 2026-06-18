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
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var nic in interfaces)
            {
                foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                    {
                        var packet = new LanDiscoveryPacket
                        {
                            ServerId = _serverId,
                            DisplayName = _displayName,
                            IpAddress = unicast.Address.ToString(),
                            Port = _serverPort
                        };

                        var payload = Encoding.UTF8.GetBytes(LanDiscoveryPacket.Serialize(packet));

                        // 1. Limited Broadcast (255.255.255.255)
                        try
                        {
                            await _udpClient!.SendAsync(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, _discoveryPort))
                                .ConfigureAwait(false);
                        }
                        catch { /* Bỏ qua lỗi định tuyến nếu có */ }

                        // 2. Directed Broadcast (ví dụ: 192.168.1.255 hoặc 26.255.255.255 cho Radmin VPN)
                        if (unicast.IPv4Mask != null)
                        {
                            try
                            {
                                var broadcastIp = GetBroadcastAddress(unicast.Address, unicast.IPv4Mask);
                                await _udpClient!.SendAsync(payload, payload.Length, new IPEndPoint(broadcastIp, _discoveryPort))
                                    .ConfigureAwait(false);
                            }
                            catch { /* Bỏ qua lỗi định tuyến */ }
                        }
                    }
                }
            }

            await Task.Delay(_broadcastInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            return IPAddress.Broadcast;

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
        }
        return new IPAddress(broadcastAddress);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
