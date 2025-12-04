namespace TextHumanizer.UI.Models;

public class DetectRequest
{
    public string Text { get; set; } = string.Empty;
}

public class DetectResponse
{
    public int AiProbability { get; set; }
    public List<string> Reasons { get; set; } = new();
}
