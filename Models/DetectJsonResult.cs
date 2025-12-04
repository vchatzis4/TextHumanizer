namespace TextHumanizer.Models;

public class DetectJsonResult
{
    public int Score { get; set; }
    public List<string>? Reasons { get; set; }
}
