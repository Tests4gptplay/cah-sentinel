using CAHSentinel;
var root = AppContext.BaseDirectory;
if (SettingsStore.DirectoryPath != root) throw new Exception("Settings must stay beside the executable.");
var settingsPath = Path.Combine(root, "settings.json");
var alertsPath = Path.Combine(root, "seen-alerts.json");
// This standalone test executable owns its output directory, never the user's install.
try
{
    SettingsStore.Save(new() { LocalRepoPath = "portable-test", PetLeft = -42 });
    var loaded = SettingsStore.Load();
    if (loaded.LocalRepoPath != "portable-test" || loaded.PetLeft != -42) throw new Exception("Settings round-trip failed.");
    SettingsStore.SaveSeen(new(StringComparer.Ordinal) { "portable-alert" });
    if (!SettingsStore.LoadSeen().SetEquals(new[] { "portable-alert" })) throw new Exception("Alerts round-trip failed.");
    if (!File.Exists(settingsPath) || !File.Exists(alertsPath)) throw new Exception("Files missing beside executable.");
    Console.WriteLine("PASS: settings.json and seen-alerts.json round-trip beside executable.");
}
finally
{
    File.Delete(settingsPath);
    File.Delete(alertsPath);
}
