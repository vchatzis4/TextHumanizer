using TextHumanizer.Models;

namespace TextHumanizer.Interfaces;

public interface IAiDetectionService
{
    AiDetectionResult CalculateAiProbability(TextAnalysisResult analysis, int? llmProbability = null);
}
