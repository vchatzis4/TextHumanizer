namespace TextHumanizer.Models.Responses;

public class SignalResponse
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public int AiLikelihood { get; set; }
    public string Interpretation { get; set; } = "";
}
