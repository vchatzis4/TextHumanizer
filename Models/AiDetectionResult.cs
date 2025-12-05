namespace TextHumanizer.Models;

public class AiDetectionResult
{
    public int AiProbability { get; set; }
    public double Confidence { get; set; }
    public List<DetectionSignal> Signals { get; set; } = new();
    public TextAnalysisResult Analysis { get; set; } = new();
    public string Summary { get; set; } = "";
}
