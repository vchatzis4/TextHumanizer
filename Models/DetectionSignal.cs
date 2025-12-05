namespace TextHumanizer.Models;

public class DetectionSignal
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public double AiLikelihood { get; set; }
    public string Interpretation { get; set; } = "";
}
