using System;

namespace LanAutoDiscovery;

public sealed class DiscoveredServer
{
    public required string ServerId { get; init; }

    public required string DisplayName { get; init; }

    public required string IpAddress { get; init; }

    public required int Port { get; init; }

    public DateTime LastSeenUtc { get; set; }

    public string Endpoint => $"{IpAddress}:{Port}";

    public override string ToString()
    {
        return $"{DisplayName} ({Endpoint})";
    }
}
