using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorCompanion.App;

public class AppConfig
{
    [JsonPropertyName("windowWidth")]
    public int WindowWidth { get; set; } = 256;

    [JsonPropertyName("windowHeight")]
    public int WindowHeight { get; set; } = 256;

    [JsonPropertyName("gravity")]
    public float Gravity { get; set; } = 4000f;

    [JsonPropertyName("terminalVelocity")]
    public float TerminalVelocity { get; set; } = 3000f;

    [JsonPropertyName("globalCooldown")]
    public float GlobalCooldown { get; set; } = 3f;

    [JsonPropertyName("sleepTimeout")]
    public float SleepTimeout { get; set; } = 90f;

    [JsonPropertyName("networkPort")]
    public int NetworkPort { get; set; } = 7777;

    [JsonPropertyName("volume")]
    public float Volume { get; set; } = 0.5f;

    [JsonPropertyName("activePack")]
    public string ActivePack { get; set; } = "default";

    [JsonPropertyName("atlasPath")]
    public string AtlasPath { get; set; } = "";

    [JsonPropertyName("atlasJsonPath")]
    public string AtlasJsonPath { get; set; } = "";

    private static string ConfigPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "assets", "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath);
            if (dir != null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
