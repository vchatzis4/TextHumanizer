using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TextHumanizer.Models;

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
