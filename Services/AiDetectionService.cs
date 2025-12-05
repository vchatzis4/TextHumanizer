using TextHumanizer.Interfaces;
using TextHumanizer.Models;

namespace TextHumanizer.Services;

public class AiDetectionService : IAiDetectionService
{
    // Thresholds derived from research on AI vs human text patterns
    private static class Thresholds
    {
        // Burstiness: humans typically -0.3 to 0.3, AI tends toward -0.5 to -0.2
        public const double BurstinessHumanMin = -0.2;
        public const double BurstinessHumanMax = 0.4;

        // Vocabulary diversity: humans 0.4-0.7, AI often 0.3-0.5
        public const double VocabDiversityHumanMin = 0.45;

        // Sentence length variance: humans have higher variance
        public const double SentenceVarianceHumanMin = 20;

        // Repetition: AI tends to repeat more
        public const double RepetitionAiThreshold = 0.1;

        // Sentence starter diversity: humans more diverse
        public const double StarterDiversityHumanMin = 0.6;

        // Contractions: humans use more
        public const double ContractionHumanMin = 0.02;
    }

    public AiDetectionResult CalculateAiProbability(TextAnalysisResult analysis, int? llmProbability = null)
    {
        var signals = new List<DetectionSignal>();
        var aiScore = 0.0;
        var totalWeight = 0.0;

        // 1. Burstiness Analysis (Weight: 15%)
        var burstinessSignal = AnalyzeBurstiness(analysis.Burstiness);
        signals.Add(burstinessSignal);
        aiScore += burstinessSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 2. Vocabulary Diversity (Weight: 15%)
        var vocabSignal = AnalyzeVocabularyDiversity(analysis.VocabularyDiversity);
        signals.Add(vocabSignal);
        aiScore += vocabSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 3. Sentence Length Variance (Weight: 12%)
        var sentenceVarSignal = AnalyzeSentenceLengthVariance(analysis.SentenceLengthVariance);
        signals.Add(sentenceVarSignal);
        aiScore += sentenceVarSignal.AiLikelihood * 0.12;
        totalWeight += 0.12;

        // 4. Repetition Score (Weight: 15%)
        var repetitionSignal = AnalyzeRepetition(analysis.RepetitionScore);
        signals.Add(repetitionSignal);
        aiScore += repetitionSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 5. Sentence Starter Diversity (Weight: 10%)
        var starterSignal = AnalyzeSentenceStarters(analysis.SentenceStarterDiversity);
        signals.Add(starterSignal);
        aiScore += starterSignal.AiLikelihood * 0.10;
        totalWeight += 0.10;

        // 6. Contraction Usage (Weight: 8%)
        var contractionSignal = AnalyzeContractions(analysis.ContractionRatio);
        signals.Add(contractionSignal);
        aiScore += contractionSignal.AiLikelihood * 0.08;
        totalWeight += 0.08;

        // 7. Word Length Variance (Weight: 5%)
        var wordLengthSignal = AnalyzeWordLengthVariance(analysis.WordLengthVariance);
        signals.Add(wordLengthSignal);
        aiScore += wordLengthSignal.AiLikelihood * 0.05;
        totalWeight += 0.05;

        // 8. LLM Analysis (Weight: 20% if available)
        if (llmProbability.HasValue)
        {
            var llmSignal = new DetectionSignal
            {
                Name = "LLM Pattern Analysis",
                Value = llmProbability.Value,
                AiLikelihood = llmProbability.Value / 100.0,
                Interpretation = llmProbability.Value > 60
                    ? "LLM detected AI-like patterns in writing style"
                    : llmProbability.Value > 40
                        ? "LLM found mixed signals in writing patterns"
                        : "LLM detected human-like writing patterns"
            };
            signals.Add(llmSignal);
            aiScore += llmSignal.AiLikelihood * 0.20;
            totalWeight += 0.20;
        }

        // Normalize score
        var normalizedScore = totalWeight > 0 ? aiScore / totalWeight : 0;

        // Apply confidence adjustment based on text length
        var confidenceMultiplier = CalculateConfidenceMultiplier(analysis.TotalWords);

        // Final probability (0-100)
        var finalProbability = (int)Math.Round(normalizedScore * 100 * confidenceMultiplier);
        finalProbability = Math.Clamp(finalProbability, 0, 100);

        return new AiDetectionResult
        {
            AiProbability = finalProbability,
            Confidence = confidenceMultiplier,
            Signals = signals,
            Analysis = analysis,
            Summary = GenerateSummary(finalProbability, signals)
        };
    }

    private DetectionSignal AnalyzeBurstiness(double burstiness)
    {
        // AI text tends to have lower (more negative) burstiness
        double aiLikelihood;
        string interpretation;

        if (burstiness < Thresholds.BurstinessHumanMin)
        {
            aiLikelihood = 0.7 + (Thresholds.BurstinessHumanMin - burstiness) * 0.5;
            interpretation = "Very uniform sentence structure typical of AI";
        }
        else if (burstiness > Thresholds.BurstinessHumanMax)
        {
            aiLikelihood = 0.2;
            interpretation = "High variability in sentence structure suggests human writing";
        }
        else
        {
            aiLikelihood = 0.5;
            interpretation = "Moderate sentence structure variation";
        }

        return new DetectionSignal
        {
            Name = "Burstiness",
            Value = Math.Round(burstiness, 3),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeVocabularyDiversity(double diversity)
    {
        double aiLikelihood;
        string interpretation;

        if (diversity < Thresholds.VocabDiversityHumanMin)
        {
            aiLikelihood = 0.6 + (Thresholds.VocabDiversityHumanMin - diversity);
            interpretation = "Limited vocabulary variety often seen in AI text";
        }
        else if (diversity > 0.6)
        {
            aiLikelihood = 0.3;
            interpretation = "Rich vocabulary suggests human writing";
        }
        else
        {
            aiLikelihood = 0.45;
            interpretation = "Moderate vocabulary diversity";
        }

        return new DetectionSignal
        {
            Name = "Vocabulary Diversity",
            Value = Math.Round(diversity * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeSentenceLengthVariance(double variance)
    {
        double aiLikelihood;
        string interpretation;

        if (variance < Thresholds.SentenceVarianceHumanMin)
        {
            aiLikelihood = 0.65;
            interpretation = "Uniform sentence lengths typical of AI";
        }
        else if (variance > 50)
        {
            aiLikelihood = 0.25;
            interpretation = "High sentence length variation suggests human writing";
        }
        else
        {
            aiLikelihood = 0.45;
            interpretation = "Moderate sentence length variation";
        }

        return new DetectionSignal
        {
            Name = "Sentence Variance",
            Value = Math.Round(variance, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeRepetition(double repetitionScore)
    {
        double aiLikelihood;
        string interpretation;

        if (repetitionScore > Thresholds.RepetitionAiThreshold)
        {
            aiLikelihood = 0.5 + repetitionScore * 3;
            interpretation = "Repetitive phrasing patterns detected";
        }
        else
        {
            aiLikelihood = 0.3;
            interpretation = "Low phrase repetition";
        }

        return new DetectionSignal
        {
            Name = "Repetition",
            Value = Math.Round(repetitionScore * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeSentenceStarters(double diversity)
    {
        double aiLikelihood;
        string interpretation;

        if (diversity < Thresholds.StarterDiversityHumanMin)
        {
            aiLikelihood = 0.6;
            interpretation = "Repetitive sentence beginnings";
        }
        else
        {
            aiLikelihood = 0.35;
            interpretation = "Varied sentence openings";
        }

        return new DetectionSignal
        {
            Name = "Starter Diversity",
            Value = Math.Round(diversity * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeContractions(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio < Thresholds.ContractionHumanMin)
        {
            aiLikelihood = 0.55;
            interpretation = "Formal style with few contractions";
        }
        else if (ratio > 0.05)
        {
            aiLikelihood = 0.3;
            interpretation = "Natural contraction usage";
        }
        else
        {
            aiLikelihood = 0.45;
            interpretation = "Moderate contraction usage";
        }

        return new DetectionSignal
        {
            Name = "Contractions",
            Value = Math.Round(ratio * 100, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeWordLengthVariance(double variance)
    {
        double aiLikelihood = variance < 5 ? 0.55 : 0.4;
        string interpretation = variance < 5
            ? "Uniform word lengths"
            : "Varied word lengths";

        return new DetectionSignal
        {
            Name = "Word Length Variance",
            Value = Math.Round(variance, 2),
            AiLikelihood = aiLikelihood,
            Interpretation = interpretation
        };
    }

    private double CalculateConfidenceMultiplier(int totalWords)
    {
        // Less confident with very short texts
        if (totalWords < 50) return 0.7;
        if (totalWords < 100) return 0.85;
        if (totalWords < 200) return 0.95;
        return 1.0;
    }

    private string GenerateSummary(int probability, List<DetectionSignal> signals)
    {
        var topSignals = signals
            .OrderByDescending(s => Math.Abs(s.AiLikelihood - 0.5))
            .Take(3)
            .ToList();

        if (probability >= 70)
            return $"High AI probability based on: {string.Join(", ", topSignals.Select(s => s.Name.ToLower()))}";
        if (probability >= 40)
            return $"Mixed signals detected. Key factors: {string.Join(", ", topSignals.Select(s => s.Name.ToLower()))}";
        return $"Likely human-written based on: {string.Join(", ", topSignals.Select(s => s.Name.ToLower()))}";
    }
}
