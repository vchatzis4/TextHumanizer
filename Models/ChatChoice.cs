using System.Text.Json.Serialization;

namespace TextHumanizer.Models;

public class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}
