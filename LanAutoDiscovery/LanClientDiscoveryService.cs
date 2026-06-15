using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanAutoDiscovery;

public sealed class LanClientDiscoveryService : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, DiscoveredServer> _servers = new();
    private readonly int _discoveryPort;
    private readonly TimeSpan _serverTimeout;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Task? _cleanupTask;

    public LanClientDiscoveryService(int discoveryPort = 40404, TimeSpan? serverTimeout = null)
    {
        _discoveryPort = discoveryPort;
        _serverTimeout = serverTimeout ?? TimeSpan.FromSeconds(10);
    }

    public event EventHandler<IReadOnlyList<DiscoveredServer>>? ServersChanged;

    public IReadOnlyList<DiscoveredServer> Servers =>
        _servers.Values
            .OrderBy(server => server.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void Start()
    {
        if (_cts is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _udpClient = CreateUdpListener();

        _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        _udpClient?.Dispose();

        await AwaitTaskSafely(_listenTask).ConfigureAwait(false);
        await AwaitTaskSafely(_cleanupTask).ConfigureAwait(false);

        _cts.Dispose();
        _udpClient = null;
        _cts = null;
        _listenTask = null;
        _cleanupTask = null;
        _servers.Clear();
        RaiseServersChanged();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient!.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                var json = Encoding.UTF8.GetString(result.Buffer);

                if (!LanDiscoveryPacket.TryDeserialize(json, out var packet) || packet is null)
                {
                    continue;
                }

                var server = _servers.AddOrUpdate(
                    packet.ServerId,
                    _ => new DiscoveredServer
                    {
                        ServerId = packet.ServerId,
                        DisplayName = packet.DisplayName,
                        IpAddress = packet.IpAddress,
                        Port = packet.Port,
                        LastSeenUtc = DateTime.UtcNow
                    },
                    (_, existing) =>
                    {
                        existing.LastSeenUtc = DateTime.UtcNow;
                        return new DiscoveredServer
                        {
                            ServerId = packet.ServerId,
                            DisplayName = packet.DisplayName,
                            IpAddress = packet.IpAddress,
                            Port = packet.Port,
                            LastSeenUtc = existing.LastSeenUtc
                        };
                    });

                _ = server;
                RaiseServersChanged();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var expiredBefore = DateTime.UtcNow - _serverTimeout;
            var changed = false;

            foreach (var pair in _servers.ToArray())
            {
                if (pair.Value.LastSeenUtc < expiredBefore)
                {
                    changed |= _servers.TryRemove(pair.Key, out _);
                }
            }

            if (changed)
            {
                RaiseServersChanged();
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
    }

    private void RaiseServersChanged()
    {
        ServersChanged?.Invoke(this, Servers);
    }

    private static async Task AwaitTaskSafely(Task? task)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private UdpClient CreateUdpListener()
    {
        var udpClient = new UdpClient
        {
            EnableBroadcast = true,
            ExclusiveAddressUse = false
        };

        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));

        return udpClient;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
