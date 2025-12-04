namespace TextHumanizer.Models.Responses;

public class HumanizeResponse
{
    public required string OriginalText { get; set; }
    public required string HumanizedText { get; set; }
}
