using System.Text.Json.Serialization;

namespace TextHumanizer.Models;

public class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice>? Choices { get; set; }
}
