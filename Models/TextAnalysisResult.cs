namespace TextHumanizer.Models;

public class TextAnalysisResult
{
    public double Burstiness { get; set; }
    public double VocabularyDiversity { get; set; }
    public double SentenceLengthVariance { get; set; }
    public double AverageSentenceLength { get; set; }
    public double WordLengthVariance { get; set; }
    public double PunctuationDensity { get; set; }
    public double RepetitionScore { get; set; }
    public double SentenceStarterDiversity { get; set; }
    public double ContractionRatio { get; set; }
    public double PersonalPronounRatio { get; set; }
    public int TotalWords { get; set; }
    public int TotalSentences { get; set; }
    public int UniqueWords { get; set; }
}
