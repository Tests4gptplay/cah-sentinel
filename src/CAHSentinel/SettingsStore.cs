using System.IO;
using System.Text.Json;

namespace CAHSentinel;

public sealed class SentinelSettings
{
    public string LocalRepoPath { get; set; } = "";
    public string SpritePackPath { get; set; } = "";
    public bool ReducedMotion { get; set; }
    public string ForegroundUrl { get; set; } = "";
    public int PollSeconds { get; set; } = 30;
    public bool Monitoring { get; set; } = true;
    public bool Notifications { get; set; } = true;
    public bool ShowPet { get; set; } = true;
    public int? PetLeft { get; set; }
    public int? PetTop { get; set; }
}

public static class SettingsStore
{
    public static string DirectoryPath { get; } = AppContext.BaseDirectory;
    public static string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    private static string AlertPath => Path.Combine(DirectoryPath, "seen-alerts.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static SentinelSettings Load()
    {
        try
        {
            var raw = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<SentinelSettings>(raw) ?? new();
            using var json = JsonDocument.Parse(raw);
            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                (json.RootElement.TryGetProperty("GitRemote", out _) ||
                 json.RootElement.TryGetProperty("Branch", out _)))
                Save(settings); // Drop legacy private-repository URL and branch from local settings.
            return settings;
        }
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
        WriteAtomic(AlertPath, JsonSerializer.Serialize(seen.Order(StringComparer.Ordinal).TakeLast(250).ToArray()));
    }

    private static void WriteAtomic(string path, string value)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, value);
        File.Move(temp, path, overwrite: true);
    }
}
