namespace TextHumanizer.Models.Responses;

public class DetectResponse
{
    public required int AiProbability { get; set; }
    public required List<string> Reasons { get; set; }
    public double Confidence { get; set; }
    public string Summary { get; set; } = "";
    public List<SignalResponse> Signals { get; set; } = new();
    public TextStatsResponse? Stats { get; set; }
}
