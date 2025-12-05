using System.Text.Json.Serialization;

namespace TextHumanizer.Models;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }
}
