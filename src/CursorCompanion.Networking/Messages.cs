using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorCompanion.Networking;

public enum MessageType : byte
{
    Hello = 1,
    ActionPing = 2,
    PositionUpdate = 3
}

public class HelloMessage
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("petSkinId")]
    public string PetSkinId { get; set; } = "default";
}

public class ActionPingMessage
{
    [JsonPropertyName("actionId")]
    public string ActionId { get; set; } = "";

    [JsonPropertyName("timestampUtcMs")]
    public long TimestampUtcMs { get; set; }
}

public class PositionUpdateMessage
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "";
}

public static class MessageSerializer
{
    public static byte[] Serialize<T>(MessageType type, T message)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message);
        var result = new byte[json.Length + 1];
        result[0] = (byte)type;
        json.CopyTo(result, 1);
        return result;
    }

    public static (MessageType type, T? message) Deserialize<T>(byte[] data)
    {
        var type = (MessageType)data[0];
        var json = data.AsSpan(1);
        var message = JsonSerializer.Deserialize<T>(json);
        return (type, message);
    }

    public static MessageType GetType(byte[] data) => (MessageType)data[0];
}
