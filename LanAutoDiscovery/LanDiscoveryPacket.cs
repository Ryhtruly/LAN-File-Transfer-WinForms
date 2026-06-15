using System.Text.Json;

namespace LanAutoDiscovery;

internal sealed class LanDiscoveryPacket
{
    public const string PacketType = "lan_server_announce_v1";

    public string Type { get; init; } = PacketType;

    public required string ServerId { get; init; }

    public required string DisplayName { get; init; }

    public required string IpAddress { get; init; }

    public required int Port { get; init; }

    public static string Serialize(LanDiscoveryPacket packet)
    {
        return JsonSerializer.Serialize(packet);
    }

    public static bool TryDeserialize(string json, out LanDiscoveryPacket? packet)
    {
        try
        {
            packet = JsonSerializer.Deserialize<LanDiscoveryPacket>(json);
            return packet is not null
                && packet.Type == PacketType
                && !string.IsNullOrWhiteSpace(packet.ServerId)
                && !string.IsNullOrWhiteSpace(packet.DisplayName)
                && !string.IsNullOrWhiteSpace(packet.IpAddress)
                && packet.Port > 0;
        }
        catch
        {
            packet = null;
            return false;
        }
    }
}
