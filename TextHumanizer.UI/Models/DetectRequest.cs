namespace TextHumanizer.UI.Models;

public class DetectRequest
{
    public string Text { get; set; } = string.Empty;
}

public class DetectResponse
{
    public int AiProbability { get; set; }
    public List<string> Reasons { get; set; } = new();
    public double Confidence { get; set; }
    public string Summary { get; set; } = "";
    public List<SignalResponse> Signals { get; set; } = new();
    public TextStatsResponse? Stats { get; set; }
}

public class SignalResponse
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public int AiLikelihood { get; set; }
    public string Interpretation { get; set; } = "";
}

public class TextStatsResponse
{
    public int TotalWords { get; set; }
    public int TotalSentences { get; set; }
    public int UniqueWords { get; set; }
    public double VocabularyDiversity { get; set; }
    public double AverageSentenceLength { get; set; }
}
