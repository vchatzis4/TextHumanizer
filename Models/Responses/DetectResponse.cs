namespace TextHumanizer.Models.Responses;

public class DetectResponse
{
    public required int AiProbability { get; set; }
    public required List<string> Reasons { get; set; }
}
