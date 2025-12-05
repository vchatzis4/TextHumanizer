using TextHumanizer.Models;

namespace TextHumanizer.Interfaces;

public interface ITextAnalysisService
{
    TextAnalysisResult Analyze(string text);
}
