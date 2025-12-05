namespace TextHumanizer.Models.Responses;

public class TextStatsResponse
{
    public int TotalWords { get; set; }
    public int TotalSentences { get; set; }
    public int UniqueWords { get; set; }
    public double VocabularyDiversity { get; set; }
    public double AverageSentenceLength { get; set; }
}
