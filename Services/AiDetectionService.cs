using TextHumanizer.Interfaces;
using TextHumanizer.Models;

namespace TextHumanizer.Services;

public class AiDetectionService : IAiDetectionService
{
    // Thresholds for English text
    private static class EnglishThresholds
    {
        public const double BurstinessHumanMin = -0.2;
        public const double BurstinessHumanMax = 0.4;
        public const double VocabDiversityHumanMin = 0.45;
        public const double SentenceVarianceHumanMin = 20;
        public const double RepetitionAiThreshold = 0.1;
        public const double StarterDiversityHumanMin = 0.6;
        public const double ContractionHumanMin = 0.02;
    }

    // Thresholds for Greek text (adjusted based on research)
    private static class GreekThresholds
    {
        public const double BurstinessHumanMin = -0.25;
        public const double BurstinessHumanMax = 0.35;
        public const double VocabDiversityHumanMin = 0.40; // Greek has more inflections
        public const double SentenceVarianceHumanMin = 25;
        public const double RepetitionAiThreshold = 0.08;
        public const double StarterDiversityHumanMin = 0.55;

        // Greek-specific AI patterns
        public const double GenericPhrasesAiThreshold = 0.5;      // phrases per 100 words
        public const double HedgingPhrasesAiThreshold = 0.3;
        public const double TransitionalRepetitionAiThreshold = 0.4; // per sentence
        public const double OverformalityAiThreshold = 1.0;       // per 100 words

        // Greek-specific human patterns
        public const double FillerWordHumanMin = 0.01;
        public const double ColloquialHumanMin = 0.5;
    }

    public AiDetectionResult CalculateAiProbability(TextAnalysisResult analysis, int? llmProbability = null)
    {
        if (analysis.IsGreekText)
        {
            return CalculateGreekAiProbability(analysis, llmProbability);
        }

        return CalculateEnglishAiProbability(analysis, llmProbability);
    }

    private AiDetectionResult CalculateEnglishAiProbability(TextAnalysisResult analysis, int? llmProbability)
    {
        var signals = new List<DetectionSignal>();
        var aiScore = 0.0;
        var totalWeight = 0.0;

        // 1. Burstiness Analysis (Weight: 15%)
        var burstinessSignal = AnalyzeBurstiness(analysis.Burstiness, false);
        signals.Add(burstinessSignal);
        aiScore += burstinessSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 2. Vocabulary Diversity (Weight: 15%)
        var vocabSignal = AnalyzeVocabularyDiversity(analysis.VocabularyDiversity, false);
        signals.Add(vocabSignal);
        aiScore += vocabSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 3. Sentence Length Variance (Weight: 12%)
        var sentenceVarSignal = AnalyzeSentenceLengthVariance(analysis.SentenceLengthVariance, false);
        signals.Add(sentenceVarSignal);
        aiScore += sentenceVarSignal.AiLikelihood * 0.12;
        totalWeight += 0.12;

        // 4. Repetition Score (Weight: 15%)
        var repetitionSignal = AnalyzeRepetition(analysis.RepetitionScore, false);
        signals.Add(repetitionSignal);
        aiScore += repetitionSignal.AiLikelihood * 0.15;
        totalWeight += 0.15;

        // 5. Sentence Starter Diversity (Weight: 10%)
        var starterSignal = AnalyzeSentenceStarters(analysis.SentenceStarterDiversity, false);
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
            var llmSignal = CreateLlmSignal(llmProbability.Value, false);
            signals.Add(llmSignal);
            aiScore += llmSignal.AiLikelihood * 0.20;
            totalWeight += 0.20;
        }

        return CreateResult(analysis, signals, aiScore, totalWeight, false);
    }

    private AiDetectionResult CalculateGreekAiProbability(TextAnalysisResult analysis, int? llmProbability)
    {
        var signals = new List<DetectionSignal>();
        var aiScore = 0.0;
        var totalWeight = 0.0;

        // Determine if formal or informal text for different signal weights
        var isFormal = analysis.IsFormalStyle;

        // === Universal signals (work for both formal and informal) ===

        // 1. Burstiness Analysis (Weight: 12%)
        var burstinessSignal = AnalyzeBurstiness(analysis.Burstiness, true);
        signals.Add(burstinessSignal);
        aiScore += burstinessSignal.AiLikelihood * 0.12;
        totalWeight += 0.12;

        // 2. Vocabulary Diversity (Weight: 10%)
        var vocabSignal = AnalyzeVocabularyDiversity(analysis.VocabularyDiversity, true);
        signals.Add(vocabSignal);
        aiScore += vocabSignal.AiLikelihood * 0.10;
        totalWeight += 0.10;

        // 3. Sentence Length Variance (Weight: 10%)
        var sentenceVarSignal = AnalyzeSentenceLengthVariance(analysis.SentenceLengthVariance, true);
        signals.Add(sentenceVarSignal);
        aiScore += sentenceVarSignal.AiLikelihood * 0.10;
        totalWeight += 0.10;

        // 4. Repetition Score (Weight: 12%)
        var repetitionSignal = AnalyzeRepetition(analysis.RepetitionScore, true);
        signals.Add(repetitionSignal);
        aiScore += repetitionSignal.AiLikelihood * 0.12;
        totalWeight += 0.12;

        // 5. Sentence Starter Diversity (Weight: 8%)
        var starterSignal = AnalyzeSentenceStarters(analysis.SentenceStarterDiversity, true);
        signals.Add(starterSignal);
        aiScore += starterSignal.AiLikelihood * 0.08;
        totalWeight += 0.08;

        if (isFormal)
        {
            // === Formal Greek text signals (AI patterns) ===

            // 6. Generic Phrases (Weight: 12%) - "σύμφωνα με έρευνες", etc.
            var genericSignal = AnalyzeGreekGenericPhrases(analysis.GreekGenericPhrasesRatio);
            signals.Add(genericSignal);
            aiScore += genericSignal.AiLikelihood * 0.12;
            totalWeight += 0.12;

            // 7. Hedging Phrases (Weight: 10%) - "Είναι σημαντικό να", etc.
            var hedgingSignal = AnalyzeGreekHedgingPhrases(analysis.GreekHedgingPhrasesRatio);
            signals.Add(hedgingSignal);
            aiScore += hedgingSignal.AiLikelihood * 0.10;
            totalWeight += 0.10;

            // 8. Transitional Repetition (Weight: 8%) - Overuse of "Επιπλέον", etc.
            var transitionalSignal = AnalyzeGreekTransitionalRepetition(analysis.GreekTransitionalRepetition);
            signals.Add(transitionalSignal);
            aiScore += transitionalSignal.AiLikelihood * 0.08;
            totalWeight += 0.08;

            // 9. Overformality (Weight: 8%) - Excessive formal vocabulary
            var overformalSignal = AnalyzeGreekOverformality(analysis.GreekOverformalityScore);
            signals.Add(overformalSignal);
            aiScore += overformalSignal.AiLikelihood * 0.08;
            totalWeight += 0.08;
        }
        else
        {
            // === Informal Greek text signals (Human patterns) ===

            // 6. Filler Words (Weight: 12%) - "λοιπόν", "δηλαδή", etc.
            var fillerSignal = AnalyzeGreekFillerWords(analysis.GreekFillerWordRatio);
            signals.Add(fillerSignal);
            aiScore += fillerSignal.AiLikelihood * 0.12;
            totalWeight += 0.12;

            // 7. Abbreviations (Weight: 8%) - "τέσπα", "δλδ", etc.
            var abbrSignal = AnalyzeGreekAbbreviations(analysis.GreekAbbreviationRatio);
            signals.Add(abbrSignal);
            aiScore += abbrSignal.AiLikelihood * 0.08;
            totalWeight += 0.08;

            // 8. Colloquial Score (Weight: 10%) - Informal expressions
            var colloquialSignal = AnalyzeGreekColloquial(analysis.GreekColloquialScore);
            signals.Add(colloquialSignal);
            aiScore += colloquialSignal.AiLikelihood * 0.10;
            totalWeight += 0.10;

            // 9. Personal Pronouns (Weight: 8%)
            var pronounSignal = AnalyzeGreekPersonalPronouns(analysis.PersonalPronounRatio);
            signals.Add(pronounSignal);
            aiScore += pronounSignal.AiLikelihood * 0.08;
            totalWeight += 0.08;
        }

        // 10. LLM Analysis (Weight: 10% - reduced for Greek as LLMs are less reliable)
        if (llmProbability.HasValue)
        {
            var llmSignal = CreateLlmSignal(llmProbability.Value, true);
            signals.Add(llmSignal);
            aiScore += llmSignal.AiLikelihood * 0.10;
            totalWeight += 0.10;
        }

        return CreateResult(analysis, signals, aiScore, totalWeight, true);
    }

    // === Signal Analysis Methods ===

    private DetectionSignal AnalyzeBurstiness(double burstiness, bool isGreek)
    {
        var minThreshold = isGreek ? GreekThresholds.BurstinessHumanMin : EnglishThresholds.BurstinessHumanMin;
        var maxThreshold = isGreek ? GreekThresholds.BurstinessHumanMax : EnglishThresholds.BurstinessHumanMax;

        double aiLikelihood;
        string interpretation;

        if (burstiness < minThreshold)
        {
            aiLikelihood = 0.7 + (minThreshold - burstiness) * 0.5;
            interpretation = isGreek
                ? "Πολύ ομοιόμορφη δομή προτάσεων, τυπικό AI"
                : "Very uniform sentence structure typical of AI";
        }
        else if (burstiness > maxThreshold)
        {
            aiLikelihood = 0.2;
            interpretation = isGreek
                ? "Υψηλή ποικιλία στη δομή προτάσεων, ανθρώπινη γραφή"
                : "High variability in sentence structure suggests human writing";
        }
        else
        {
            aiLikelihood = 0.5;
            interpretation = isGreek
                ? "Μέτρια ποικιλία στη δομή προτάσεων"
                : "Moderate sentence structure variation";
        }

        return new DetectionSignal
        {
            Name = isGreek ? "Εκρηκτικότητα" : "Burstiness",
            Value = Math.Round(burstiness, 3),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeVocabularyDiversity(double diversity, bool isGreek)
    {
        var threshold = isGreek ? GreekThresholds.VocabDiversityHumanMin : EnglishThresholds.VocabDiversityHumanMin;

        double aiLikelihood;
        string interpretation;

        if (diversity < threshold)
        {
            aiLikelihood = 0.6 + (threshold - diversity);
            interpretation = isGreek
                ? "Περιορισμένη ποικιλία λεξιλογίου, συνηθισμένο σε AI"
                : "Limited vocabulary variety often seen in AI text";
        }
        else if (diversity > 0.6)
        {
            aiLikelihood = 0.3;
            interpretation = isGreek
                ? "Πλούσιο λεξιλόγιο, δείχνει ανθρώπινη γραφή"
                : "Rich vocabulary suggests human writing";
        }
        else
        {
            aiLikelihood = 0.45;
            interpretation = isGreek
                ? "Μέτρια ποικιλία λεξιλογίου"
                : "Moderate vocabulary diversity";
        }

        return new DetectionSignal
        {
            Name = isGreek ? "Ποικιλία Λεξιλογίου" : "Vocabulary Diversity",
            Value = Math.Round(diversity * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeSentenceLengthVariance(double variance, bool isGreek)
    {
        var threshold = isGreek ? GreekThresholds.SentenceVarianceHumanMin : EnglishThresholds.SentenceVarianceHumanMin;

        double aiLikelihood;
        string interpretation;

        if (variance < threshold)
        {
            aiLikelihood = 0.65;
            interpretation = isGreek
                ? "Ομοιόμορφο μήκος προτάσεων, τυπικό AI"
                : "Uniform sentence lengths typical of AI";
        }
        else if (variance > 50)
        {
            aiLikelihood = 0.25;
            interpretation = isGreek
                ? "Υψηλή διακύμανση μήκους, δείχνει ανθρώπινη γραφή"
                : "High sentence length variation suggests human writing";
        }
        else
        {
            aiLikelihood = 0.45;
            interpretation = isGreek
                ? "Μέτρια διακύμανση μήκους προτάσεων"
                : "Moderate sentence length variation";
        }

        return new DetectionSignal
        {
            Name = isGreek ? "Διακύμανση Προτάσεων" : "Sentence Variance",
            Value = Math.Round(variance, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeRepetition(double repetitionScore, bool isGreek)
    {
        var threshold = isGreek ? GreekThresholds.RepetitionAiThreshold : EnglishThresholds.RepetitionAiThreshold;

        double aiLikelihood;
        string interpretation;

        if (repetitionScore > threshold)
        {
            aiLikelihood = 0.5 + repetitionScore * 3;
            interpretation = isGreek
                ? "Επαναλαμβανόμενες φράσεις εντοπίστηκαν"
                : "Repetitive phrasing patterns detected";
        }
        else
        {
            aiLikelihood = 0.3;
            interpretation = isGreek
                ? "Χαμηλή επανάληψη φράσεων"
                : "Low phrase repetition";
        }

        return new DetectionSignal
        {
            Name = isGreek ? "Επανάληψη" : "Repetition",
            Value = Math.Round(repetitionScore * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeSentenceStarters(double diversity, bool isGreek)
    {
        var threshold = isGreek ? GreekThresholds.StarterDiversityHumanMin : EnglishThresholds.StarterDiversityHumanMin;

        double aiLikelihood;
        string interpretation;

        if (diversity < threshold)
        {
            aiLikelihood = 0.6;
            interpretation = isGreek
                ? "Επαναλαμβανόμενες αρχές προτάσεων"
                : "Repetitive sentence beginnings";
        }
        else
        {
            aiLikelihood = 0.35;
            interpretation = isGreek
                ? "Ποικίλες αρχές προτάσεων"
                : "Varied sentence openings";
        }

        return new DetectionSignal
        {
            Name = isGreek ? "Ποικιλία Αρχών" : "Starter Diversity",
            Value = Math.Round(diversity * 100, 1),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeContractions(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio < EnglishThresholds.ContractionHumanMin)
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

    // === Greek-specific signal analyzers ===

    private DetectionSignal AnalyzeGreekGenericPhrases(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > GreekThresholds.GenericPhrasesAiThreshold)
        {
            aiLikelihood = 0.7 + ratio * 0.1;
            interpretation = "Γενικές φράσεις χωρίς συγκεκριμένες πηγές (π.χ. 'σύμφωνα με έρευνες')";
        }
        else if (ratio > 0)
        {
            aiLikelihood = 0.5;
            interpretation = "Κάποιες γενικές αναφορές";
        }
        else
        {
            aiLikelihood = 0.3;
            interpretation = "Συγκεκριμένες αναφορές ή καθόλου γενικές φράσεις";
        }

        return new DetectionSignal
        {
            Name = "Γενικές Φράσεις",
            Value = Math.Round(ratio, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekHedgingPhrases(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > GreekThresholds.HedgingPhrasesAiThreshold)
        {
            aiLikelihood = 0.65 + ratio * 0.15;
            interpretation = "Υπερβολική χρήση εισαγωγικών φράσεων (π.χ. 'Είναι σημαντικό να')";
        }
        else if (ratio > 0)
        {
            aiLikelihood = 0.45;
            interpretation = "Μέτρια χρήση εισαγωγικών φράσεων";
        }
        else
        {
            aiLikelihood = 0.35;
            interpretation = "Άμεσο ύφος χωρίς περιττές εισαγωγές";
        }

        return new DetectionSignal
        {
            Name = "Εισαγωγικές Φράσεις",
            Value = Math.Round(ratio, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekTransitionalRepetition(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > GreekThresholds.TransitionalRepetitionAiThreshold)
        {
            aiLikelihood = 0.6 + ratio * 0.2;
            interpretation = "Επαναλαμβανόμενες μεταβατικές φράσεις (Επιπλέον, Επιπροσθέτως, κλπ.)";
        }
        else if (ratio > 0.2)
        {
            aiLikelihood = 0.45;
            interpretation = "Μέτρια χρήση μεταβατικών φράσεων";
        }
        else
        {
            aiLikelihood = 0.35;
            interpretation = "Φυσική ροή χωρίς υπερβολικές μεταβάσεις";
        }

        return new DetectionSignal
        {
            Name = "Μεταβατικές Φράσεις",
            Value = Math.Round(ratio, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekOverformality(double score)
    {
        double aiLikelihood;
        string interpretation;

        if (score > GreekThresholds.OverformalityAiThreshold)
        {
            aiLikelihood = 0.6 + score * 0.05;
            interpretation = "Υπερβολικά επίσημο λεξιλόγιο (καίριος, θεμελιώδης, κλπ.)";
        }
        else if (score > 0.5)
        {
            aiLikelihood = 0.45;
            interpretation = "Επίσημο αλλά φυσικό ύφος";
        }
        else
        {
            aiLikelihood = 0.35;
            interpretation = "Φυσικό επίπεδο επισημότητας";
        }

        return new DetectionSignal
        {
            Name = "Υπερ-επισημότητα",
            Value = Math.Round(score, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekFillerWords(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > GreekThresholds.FillerWordHumanMin)
        {
            aiLikelihood = 0.25;
            interpretation = "Φυσική χρήση filler words (λοιπόν, δηλαδή, κλπ.)";
        }
        else
        {
            aiLikelihood = 0.6;
            interpretation = "Απουσία filler words, πιθανόν AI";
        }

        return new DetectionSignal
        {
            Name = "Filler Words",
            Value = Math.Round(ratio * 100, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekAbbreviations(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > 0)
        {
            aiLikelihood = 0.2;
            interpretation = "Χρήση συντομεύσεων (τέσπα, δλδ, κλπ.), ανθρώπινο στοιχείο";
        }
        else
        {
            aiLikelihood = 0.55;
            interpretation = "Καμία συντόμευση, πιθανόν AI";
        }

        return new DetectionSignal
        {
            Name = "Συντομεύσεις",
            Value = Math.Round(ratio * 100, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekColloquial(double score)
    {
        double aiLikelihood;
        string interpretation;

        if (score > GreekThresholds.ColloquialHumanMin)
        {
            aiLikelihood = 0.2;
            interpretation = "Χρήση καθομιλουμένης, φυσικό ανθρώπινο ύφος";
        }
        else if (score > 0)
        {
            aiLikelihood = 0.4;
            interpretation = "Κάποια στοιχεία καθομιλουμένης";
        }
        else
        {
            aiLikelihood = 0.6;
            interpretation = "Απουσία καθομιλουμένης, πιθανόν AI";
        }

        return new DetectionSignal
        {
            Name = "Καθομιλουμένη",
            Value = Math.Round(score, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal AnalyzeGreekPersonalPronouns(double ratio)
    {
        double aiLikelihood;
        string interpretation;

        if (ratio > 0.05)
        {
            aiLikelihood = 0.3;
            interpretation = "Προσωπικό ύφος με χρήση αντωνυμιών";
        }
        else if (ratio > 0.02)
        {
            aiLikelihood = 0.45;
            interpretation = "Μέτρια χρήση προσωπικών αντωνυμιών";
        }
        else
        {
            aiLikelihood = 0.6;
            interpretation = "Απρόσωπο ύφος, τυπικό AI";
        }

        return new DetectionSignal
        {
            Name = "Προσωπικές Αντωνυμίες",
            Value = Math.Round(ratio * 100, 2),
            AiLikelihood = Math.Clamp(aiLikelihood, 0, 1),
            Interpretation = interpretation
        };
    }

    private DetectionSignal CreateLlmSignal(int llmProbability, bool isGreek)
    {
        return new DetectionSignal
        {
            Name = isGreek ? "Ανάλυση LLM" : "LLM Pattern Analysis",
            Value = llmProbability,
            AiLikelihood = llmProbability / 100.0,
            Interpretation = llmProbability > 60
                ? (isGreek ? "Το LLM εντόπισε χαρακτηριστικά AI" : "LLM detected AI-like patterns in writing style")
                : llmProbability > 40
                    ? (isGreek ? "Μικτά σήματα από το LLM" : "LLM found mixed signals in writing patterns")
                    : (isGreek ? "Το LLM εντόπισε ανθρώπινο ύφος" : "LLM detected human-like writing patterns")
        };
    }

    private AiDetectionResult CreateResult(TextAnalysisResult analysis, List<DetectionSignal> signals,
        double aiScore, double totalWeight, bool isGreek)
    {
        var normalizedScore = totalWeight > 0 ? aiScore / totalWeight : 0;
        var confidenceMultiplier = CalculateConfidenceMultiplier(analysis.TotalWords);

        var finalProbability = (int)Math.Round(normalizedScore * 100 * confidenceMultiplier);
        finalProbability = Math.Clamp(finalProbability, 0, 100);

        return new AiDetectionResult
        {
            AiProbability = finalProbability,
            Confidence = confidenceMultiplier,
            Signals = signals,
            Analysis = analysis,
            Summary = GenerateSummary(finalProbability, signals, isGreek)
        };
    }

    private double CalculateConfidenceMultiplier(int totalWords)
    {
        if (totalWords < 50) return 0.7;
        if (totalWords < 100) return 0.85;
        if (totalWords < 200) return 0.95;
        return 1.0;
    }

    private string GenerateSummary(int probability, List<DetectionSignal> signals, bool isGreek)
    {
        var topSignals = signals
            .OrderByDescending(s => Math.Abs(s.AiLikelihood - 0.5))
            .Take(3)
            .ToList();

        var signalNames = string.Join(", ", topSignals.Select(s => s.Name.ToLower()));

        if (isGreek)
        {
            if (probability >= 70)
                return $"Υψηλή πιθανότητα AI βάσει: {signalNames}";
            if (probability >= 40)
                return $"Μικτά σήματα. Βασικοί παράγοντες: {signalNames}";
            return $"Πιθανόν ανθρώπινο κείμενο βάσει: {signalNames}";
        }

        if (probability >= 70)
            return $"High AI probability based on: {signalNames}";
        if (probability >= 40)
            return $"Mixed signals detected. Key factors: {signalNames}";
        return $"Likely human-written based on: {signalNames}";
    }
}
