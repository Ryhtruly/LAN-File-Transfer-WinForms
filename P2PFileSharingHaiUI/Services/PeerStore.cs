using System.Text.Json;
using P2PFileSharingHaiUI.Models;

namespace P2PFileSharingHaiUI.Services;

public sealed class PeerStore
{
    private readonly string filePath;
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    public PeerStore()
    {
        filePath = Path.Combine(AppContext.BaseDirectory, "peers.json");
    }

    public List<PeerInfo> Load()
    {
        if (!File.Exists(filePath))
        {
            return
            [
                new PeerInfo { DisplayName = "Phòng của Hải", IpAddress = "127.0.0.1", Port = 8888 },
                new PeerInfo { DisplayName = "Phòng của Tú", IpAddress = "192.168.1.5", Port = 8888 },
                new PeerInfo { DisplayName = "Phòng của Dũng", IpAddress = "192.168.1.6", Port = 8888 }
            ];
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<PeerInfo> peers = JsonSerializer.Deserialize<List<PeerInfo>>(json) ?? [];
            foreach (PeerInfo peer in peers)
                peer.DisplayName = NormalizeDisplayName(peer.DisplayName);

            return peers;
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<PeerInfo> peers)
    {
        string json = JsonSerializer.Serialize(peers, jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static string NormalizeDisplayName(string name)
    {
        return name switch
        {
            "Room cua Hai" => "Phòng của Hải",
            "Room cua Tu" => "Phòng của Tú",
            "Room cua Dung" => "Phòng của Dũng",
            _ => name
        };
    }
}
