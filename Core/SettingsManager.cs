using System.Text.Json;
using System.IO;
using System.Reflection;

namespace P2PFileSharingApp.Core;

public class AppSettings
{
    public bool IsFirstRun { get; set; } = true;
    public string DisplayName { get; set; } = Environment.MachineName;
    public string DefaultDownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "PeerLink");
}

public static class SettingsManager
{
    private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    
    public static AppSettings Current { get; private set; } = new AppSettings();

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var config = JsonSerializer.Deserialize<AppSettings>(json);
                if (config != null)
                {
                    Current = config;
                }
            }
        }
        catch
        {
            Current = new AppSettings();
        }
        
        // Ensure default download path exists
        if (!Directory.Exists(Current.DefaultDownloadPath))
        {
            try { Directory.CreateDirectory(Current.DefaultDownloadPath); } catch { }
        }
    }

    public static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
