using System.Text.Json.Serialization;

namespace CursorCompanion.Actions;

public class ActionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("clipName")]
    public string ClipName { get; set; } = "";

    [JsonPropertyName("soundFile")]
    public string? SoundFile { get; set; }

    [JsonPropertyName("cooldownOverride")]
    public float? CooldownOverride { get; set; }
}

public class ActionPack
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("actionIds")]
    public List<string> ActionIds { get; set; } = new();
}

public class ActionsData
{
    [JsonPropertyName("actions")]
    public List<ActionDefinition> Actions { get; set; } = new();
}

public class PacksData
{
    [JsonPropertyName("packs")]
    public List<ActionPack> Packs { get; set; } = new();
}
