namespace P2PFileSharingApp.Models;

public sealed class PeerInfo
{
    public string DisplayName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 8888;
    public DateTime LastSeen { get; set; } = DateTime.Now;

    public string Key => $"{IpAddress}:{Port}";

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            return $"{IpAddress}:{Port}";

        return $"{DisplayName} ({IpAddress}:{Port})";
    }
}
