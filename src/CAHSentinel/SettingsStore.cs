using System.IO;
using System.Text.Json;

namespace CAHSentinel;

public sealed class SentinelSettings
{
    public string GitRemote { get; set; } = "";
    public string Branch { get; set; } = "main";
    public string ForegroundUrl { get; set; } = "";
    public int PollSeconds { get; set; } = 30;
    public bool Monitoring { get; set; } = true;
    public bool Notifications { get; set; } = true;
    public bool ShowPet { get; set; } = true;
}

public static class SettingsStore
{
    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CAHSentinel");
    public static string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    private static string AlertPath => Path.Combine(DirectoryPath, "seen-alerts.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static SentinelSettings Load()
    {
        try { return JsonSerializer.Deserialize<SentinelSettings>(File.ReadAllText(SettingsPath)) ?? new(); }
        catch (IOException) { return new(); }
        catch (JsonException) { return new(); }
    }

    public static void Save(SentinelSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        WriteAtomic(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static HashSet<string> LoadSeen()
    {
        try { return new HashSet<string>(
            JsonSerializer.Deserialize<List<string>>(File.ReadAllText(AlertPath)) ?? [], StringComparer.Ordinal); }
        catch (IOException) { return new(StringComparer.Ordinal); }
        catch (JsonException) { return new(StringComparer.Ordinal); }
    }

    public static void SaveSeen(HashSet<string> seen)
    {
        Directory.CreateDirectory(DirectoryPath);
        // Keep the disk file bounded even after many task transitions.
        WriteAtomic(AlertPath, JsonSerializer.Serialize(seen.Order(StringComparer.Ordinal).TakeLast(250).ToArray()));
    }

    private static void WriteAtomic(string path, string value)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, value);
        File.Move(temp, path, overwrite: true);
    }
}
