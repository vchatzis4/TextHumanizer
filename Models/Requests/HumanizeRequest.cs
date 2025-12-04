using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TextHumanizer.Models.Requests;

public class HumanizeRequest
{
    [Required]
    [MinLength(1)]
    public required string Text { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Tone Tone { get; set; }
}

public enum Tone
{
    Casual,
    Formal,
    Academic
}
