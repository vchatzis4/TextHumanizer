namespace TextHumanizer.UI.Models;

public class HumanizeRequest
{
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = "Casual";
}

public class HumanizeResponse
{
    public string OriginalText { get; set; } = string.Empty;
    public string HumanizedText { get; set; } = string.Empty;
}
